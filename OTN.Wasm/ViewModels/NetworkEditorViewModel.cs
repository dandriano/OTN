using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTN.Core;
using OTN.Enums;
using OTN.Extensions;
using QuikGraph;

namespace OTN.Wasm.ViewModels;

/// <summary>UI-independent commands and state for editing and routing an optical graph.</summary>
public sealed class NetworkEditorViewModel : ObservableObject
{
    private readonly List<NodeViewModel> _nodes = [];
    private readonly List<LinkViewModel> _links = [];
    private readonly HashSet<NodeViewModel> _requiredRouteNodes = [];
    private readonly HashSet<NodeViewModel> _avoidedRouteNodes = [];
    private NodeViewModel? _selectedNode;
    private LinkViewModel? _selectedLink;
    private NodeViewModel? _pendingLinkSource;
    private IReadOnlyList<RouteViewModel> _routes = [];
    private int _selectedRouteIndex = -1;
    private string? _routeError;
    private bool _isRouting;
    private int _revision;

    public Network Network { get; } = new();
    public IRelayCommand CancelPendingLinkCommand { get; }
    public IRelayCommand<int> SelectRouteCommand { get; }
    public IReadOnlyList<NodeViewModel> Nodes => _nodes;
    public IReadOnlyList<LinkViewModel> Links => _links;
    public IReadOnlyCollection<NodeViewModel> RequiredRouteNodes => _requiredRouteNodes;
    public IReadOnlyCollection<NodeViewModel> AvoidedRouteNodes => _avoidedRouteNodes;
    public NodeViewModel? SelectedNode { get => _selectedNode; private set => SetProperty(ref _selectedNode, value); }
    public LinkViewModel? SelectedLink { get => _selectedLink; private set => SetProperty(ref _selectedLink, value); }
    public NodeViewModel? PendingLinkSource { get => _pendingLinkSource; private set => SetProperty(ref _pendingLinkSource, value); }
    public IReadOnlyList<RouteViewModel> Routes { get => _routes; private set => SetProperty(ref _routes, value); }
    public int SelectedRouteIndex { get => _selectedRouteIndex; private set => SetProperty(ref _selectedRouteIndex, value); }
    public string? RouteError { get => _routeError; private set => SetProperty(ref _routeError, value); }
    public bool IsRouting { get => _isRouting; private set => SetProperty(ref _isRouting, value); }
    public int Revision { get => _revision; private set => SetProperty(ref _revision, value); }

    public NetworkEditorViewModel()
    {
        CancelPendingLinkCommand = new RelayCommand(CancelPendingLink);
        SelectRouteCommand = new RelayCommand<int>(SelectRoute);
    }

    public NodeViewModel AddNode(NetNodeType type, double x, double y)
    {
        var node = (NetNode)Network.Add(type);
        var editorNode = new NodeViewModel(node, x, y);
        _nodes.Add(editorNode);
        SelectNode(editorNode);
        GraphChanged();
        return editorNode;
    }

    public void SelectNode(NodeViewModel? node)
    {
        SelectedNode = node;
        SelectedLink = null;
        OnPropertyChanged(nameof(SelectedElement));
    }

    public void SelectLink(LinkViewModel? link)
    {
        SelectedLink = link;
        SelectedNode = null;
        OnPropertyChanged(nameof(SelectedElement));
    }

    public object? SelectedElement => (object?)SelectedNode ?? SelectedLink;

    public void MoveNode(NodeViewModel node, double x, double y)
    {
        if (!_nodes.Contains(node)) 
            return;
        node.X = x;
        node.Y = y;
        OnPropertyChanged(nameof(Nodes));
    }

    public bool IsRequiredRouteNode(NodeViewModel node) => _requiredRouteNodes.Contains(node);
    public bool IsAvoidedRouteNode(NodeViewModel node) => _avoidedRouteNodes.Contains(node);

    public void SetRequiredRouteNode(NodeViewModel node, bool required)
    {
        EnsureNode(node);
        var changed = required ? _requiredRouteNodes.Add(node) : _requiredRouteNodes.Remove(node);
        if (!changed) 
            return;
        if (required)
            _avoidedRouteNodes.Remove(node);
        RouteConstraintsChanged();
    }

    public void SetAvoidedRouteNode(NodeViewModel node, bool avoided)
    {
        EnsureNode(node);
        var changed = avoided ? _avoidedRouteNodes.Add(node) : _avoidedRouteNodes.Remove(node);
        if (!changed) 
            return;
        if (avoided)
            _requiredRouteNodes.Remove(node);
        RouteConstraintsChanged();
    }

    public void ToggleRequiredRouteNode(NodeViewModel node) => SetRequiredRouteNode(node, !IsRequiredRouteNode(node));
    public void ToggleAvoidedRouteNode(NodeViewModel node) => SetAvoidedRouteNode(node, !IsAvoidedRouteNode(node));

    public void ToggleRouteConstraint(NodeViewModel node, bool required)
    {
        if (required) 
            ToggleRequiredRouteNode(node);
        else 
            ToggleAvoidedRouteNode(node);
    }

    public void BeginLink(NodeViewModel source)
    {
        if (_nodes.Contains(source)) 
            PendingLinkSource = source;
    }

    public void CancelPendingLink() => PendingLinkSource = null;

    public bool CompleteLink(NodeViewModel target, double weight, LinkType type)
    {
        if (PendingLinkSource is not { } source) 
            return false;
        PendingLinkSource = null;
        if (ReferenceEquals(source, target)) 
            return false;
        AddLink(source, target, weight, type);
        return true;
    }

    public LinkViewModel AddLink(NodeViewModel source, NodeViewModel target, double weight, LinkType type)
    {
        if (!_nodes.Contains(source) || !_nodes.Contains(target))
            throw new ArgumentException("Both link endpoints must belong to this editor document.");
        if (ReferenceEquals(source, target))
            throw new ArgumentException("A node cannot be linked to itself.", nameof(target));
        if (!double.IsFinite(weight) || weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Link weight must be a finite non-negative value.");

        var link = Network.Add(source.Node, target.Node, weight, type);

        var editorLink = new LinkViewModel(link);
        _links.Add(editorLink);
        SelectLink(editorLink);
        GraphChanged();
        return editorLink;
    }

    public void SetNodeType(NodeViewModel node, NetNodeType type)
    {
        EnsureNode(node);
        node.Node.Type = type;
        GraphChanged();
    }

    public void UpdateLink(LinkViewModel editorLink, double weight, LinkType type)
    {
        ArgumentNullException.ThrowIfNull(editorLink);
        if (!_links.Contains(editorLink))
            throw new ArgumentException("The link is not part of this editor document.", nameof(editorLink));
        if (!double.IsFinite(weight) || weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Link weight must be a finite non-negative value.");

        editorLink.Link = Network.Update(editorLink.Link, weight, type);
        GraphChanged();
        OnPropertyChanged(nameof(Links));
    }

    public void SelectRoute(int index)
    {
        SelectedRouteIndex = index >= 0 && index < Routes.Count ? index : -1;
    }

    public void FindOpticRoutes(Guid sourceId, Guid targetId, int k)
    {
        ExecuteRouteSearch(() => Network.Optical.FindOpticPath(FindNode(sourceId), FindNode(targetId), RouteConstraints, k)
            .Select(path => new RouteViewModel(path, path.Sum(link => link.Tag))).ToArray());
    }

    public void PrepareRouteSearch()
    {
        ClearRoutes();
        IsRouting = true;
    }

    private void ExecuteRouteSearch(Func<IReadOnlyList<RouteViewModel>> find)
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

    private NetNode FindNode(Guid id) 
        => _nodes.Select(item => item.Node).FirstOrDefault(node => node.Id == id)
            ?? throw new ArgumentException("Select source and target nodes from the current graph.");

    private void EnsureNode(NodeViewModel node)
    {
        if (!_nodes.Contains(node)) 
            throw new ArgumentException("The node is not part of this editor document.", nameof(node));
    }

    private void GraphChanged()
    {
        ClearRoutes();
        Revision++;
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Links));
    }

    private void RouteConstraintsChanged()
    {
        ClearRoutes();
        OnPropertyChanged(nameof(RequiredRouteNodes));
        OnPropertyChanged(nameof(AvoidedRouteNodes));
        OnPropertyChanged(nameof(RouteConstraints));
    }

    public RouteConstraints RouteConstraints => new(
        _requiredRouteNodes.Select(node => node.Node),
        _avoidedRouteNodes.Select(node => node.Node));

    private void ClearRoutes()
    {
        Routes = [];
        SelectedRouteIndex = -1;
        RouteError = null;
    }
}
