using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTN.Core;
using OTN.Enums;
using OTN.Extensions;
using QuikGraph;
using QuikGraph.Algorithms;

namespace OTN.Editor;

/// <summary>UI-independent commands and state for editing and routing an optical graph.</summary>
public sealed class EditorDocument : ObservableObject
{
    private readonly List<EditorNode> _nodes = [];
    private readonly List<EditorLink> _links = [];
    private EditorNode? _selectedNode;
    private EditorLink? _selectedLink;
    private EditorNode? _pendingLinkSource;
    private IReadOnlyList<RouteResult> _routes = [];
    private int _selectedRouteIndex = -1;
    private string? _routeError;
    private bool _isRouting;
    private int _revision;

    public Network Network { get; } = new();
    public IRelayCommand CancelPendingLinkCommand { get; }
    public IRelayCommand<int> SelectRouteCommand { get; }
    public IReadOnlyList<EditorNode> Nodes => _nodes;
    public IReadOnlyList<EditorLink> Links => _links;
    public EditorNode? SelectedNode { get => _selectedNode; private set => SetProperty(ref _selectedNode, value); }
    public EditorLink? SelectedLink { get => _selectedLink; private set => SetProperty(ref _selectedLink, value); }
    public EditorNode? PendingLinkSource { get => _pendingLinkSource; private set => SetProperty(ref _pendingLinkSource, value); }
    public IReadOnlyList<RouteResult> Routes { get => _routes; private set => SetProperty(ref _routes, value); }
    public int SelectedRouteIndex { get => _selectedRouteIndex; private set => SetProperty(ref _selectedRouteIndex, value); }
    public string? RouteError { get => _routeError; private set => SetProperty(ref _routeError, value); }
    public bool IsRouting { get => _isRouting; private set => SetProperty(ref _isRouting, value); }
    public int Revision { get => _revision; private set => SetProperty(ref _revision, value); }

    public EditorDocument()
    {
        CancelPendingLinkCommand = new RelayCommand(CancelPendingLink);
        SelectRouteCommand = new RelayCommand<int>(SelectRoute);
    }

    public EditorNode AddNode(NetNodeType type, double x, double y)
    {
        var node = (NetNode)Network.AddNetNode(type);
        var editorNode = new EditorNode(node, x, y);
        _nodes.Add(editorNode);
        SelectNode(editorNode);
        GraphChanged();
        return editorNode;
    }

    public void SelectNode(EditorNode? node)
    {
        SelectedNode = node;
        SelectedLink = null;
        OnPropertyChanged(nameof(SelectedElement));
    }

    public void SelectLink(EditorLink? link)
    {
        SelectedLink = link;
        SelectedNode = null;
        OnPropertyChanged(nameof(SelectedElement));
    }

    public object? SelectedElement => (object?)SelectedNode ?? SelectedLink;

    public void MoveNode(EditorNode node, double x, double y)
    {
        if (!_nodes.Contains(node)) return;
        node.X = x;
        node.Y = y;
        OnPropertyChanged(nameof(Nodes));
    }

    public void SetCtrlTag(EditorNode node, bool tagged)
    {
        if (!_nodes.Contains(node)) return;
        node.CtrlTagged = tagged;
        OnPropertyChanged(nameof(Nodes));
    }

    public void BeginLink(EditorNode source)
    {
        if (_nodes.Contains(source)) PendingLinkSource = source;
    }

    public void CancelPendingLink() => PendingLinkSource = null;

    public bool CompleteLink(EditorNode target, double weight, LinkType type)
    {
        if (PendingLinkSource is not { } source) return false;
        PendingLinkSource = null;
        if (ReferenceEquals(source, target)) return false;
        AddLink(source, target, weight, type);
        return true;
    }

    public EditorLink AddLink(EditorNode source, EditorNode target, double weight, LinkType type)
    {
        if (!_nodes.Contains(source) || !_nodes.Contains(target))
            throw new ArgumentException("Both link endpoints must belong to this editor document.");
        if (ReferenceEquals(source, target))
            throw new ArgumentException("A node cannot be linked to itself.", nameof(target));
        if (!double.IsFinite(weight) || weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Link weight must be a finite non-negative value.");

        var link = (Link)Network.AddLink(source.Node, target.Node, weight, type);
        var editorLink = new EditorLink(link);
        _links.Add(editorLink);
        SelectLink(editorLink);
        GraphChanged();
        return editorLink;
    }

    public void SetNodeType(EditorNode node, NetNodeType type)
    {
        EnsureNode(node);
        node.Node.SetType(type);
        GraphChanged();
    }

    public void SetRoutingRole(EditorNode node, RouteNodeType role)
    {
        EnsureNode(node);
        node.Node.SetRouteRole(role);
        GraphChanged();
    }

    public void UpdateLink(EditorLink editorLink, double weight, LinkType type)
    {
        if (!_links.Contains(editorLink)) throw new ArgumentException("The link is not part of this editor document.", nameof(editorLink));
        if (!double.IsFinite(weight) || weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Link weight must be a finite non-negative value.");

        var oldLink = editorLink.Link;
        var oldReverse = oldLink.Reverse;
        Network.Optical.RemoveEdge(oldLink);
        if (oldReverse is not null && Network.Optical.ContainsEdge(oldReverse))
            Network.Optical.RemoveEdge(oldReverse);

        editorLink.Link = (Link)Network.AddLink(oldLink.Source, oldLink.Target, weight, type);
        GraphChanged();
        OnPropertyChanged(nameof(Links));
    }

    public void SelectRoute(int index)
    {
        SelectedRouteIndex = index >= 0 && index < Routes.Count ? index : -1;
    }

    public void FindKRoutes(Guid sourceId, Guid targetId, int k)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
        FindRoutes(() => Network.Optical.FindOpticPath(FindNode(sourceId), FindNode(targetId), k)
            .Select(path => new RouteResult(path, path.Sum(link => link.Tag))).ToArray());
    }

    public void PrepareRouteSearch()
    {
        ClearRoutes();
        IsRouting = true;
    }

    public void FindMustPassRoute(Guid sourceId, Guid targetId)
    {
        FindRoutes(() =>
        {
            var path = Network.Optical.FindMustPassOpticPath(FindNode(sourceId), FindNode(targetId));
            return [new RouteResult(path, path.Sum(link => link.Tag))];
        });
    }

    private void FindRoutes(Func<IReadOnlyList<RouteResult>> find)
    {
        ClearRoutes();
        IsRouting = true;
        try
        {
            Routes = find();
            SelectedRouteIndex = Routes.Count > 0 ? 0 : -1;
        }
        catch (NoPathFoundException exception)
        {
            RouteError = exception.Message;
        }
        catch (ArgumentException exception)
        {
            RouteError = exception.Message;
        }
        finally
        {
            IsRouting = false;
        }
    }

    private NetNode FindNode(Guid id) => _nodes.Select(item => item.Node).FirstOrDefault(node => node.Id == id)
        ?? throw new ArgumentException("Select source and target nodes from the current graph.");

    private void EnsureNode(EditorNode node)
    {
        if (!_nodes.Contains(node)) throw new ArgumentException("The node is not part of this editor document.", nameof(node));
    }

    private void GraphChanged()
    {
        ClearRoutes();
        Revision++;
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Links));
    }

    private void ClearRoutes()
    {
        Routes = [];
        SelectedRouteIndex = -1;
        RouteError = null;
    }
}
