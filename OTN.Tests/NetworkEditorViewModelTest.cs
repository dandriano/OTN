using OTN.Wasm.ViewModels;
using OTN.Enums;

namespace OTN.Tests;

public class NetworkEditorViewModelTest
{
    [Fact]
    public void AddNode_StoresCanvasPositionAndSelectsNode()
    {
        var document = new NetworkEditorViewModel();

        var node = document.AddNode(NetNodeType.ROADM, 123, 456);

        Assert.Same(node, document.SelectedNode);
        Assert.Equal((123d, 456d), (node.X, node.Y));
        Assert.Single(document.Network.Optical.Vertices);
    }

    [Fact]
    public void SetNodeType_UpdatesTheOtnNode()
    {
        var document = new NetworkEditorViewModel();
        var node = document.AddNode(NetNodeType.Terminal, 10, 20);

        document.SetNodeType(node, NetNodeType.ROADM);

        Assert.Equal(NetNodeType.ROADM, node.Node.Type);
    }

    [Fact]
    public void PendingLink_CanBeCancelledAndRejectsSelfLink()
    {
        var document = new NetworkEditorViewModel();
        var node = document.AddNode(NetNodeType.Terminal, 0, 0);

        document.BeginLink(node);
        Assert.False(document.CompleteLink(node, 10, LinkType.Directed));
        Assert.Null(document.PendingLinkSource);
        Assert.Empty(document.Links);

        document.BeginLink(node);
        document.CancelPendingLinkCommand.Execute(null);
        Assert.Null(document.PendingLinkSource);
        Assert.Empty(document.Links);
    }

    [Fact]
    public void AddLink_CreatesDirectedAndUndirectedEdges()
    {
        var document = new NetworkEditorViewModel();
        var first = document.AddNode(NetNodeType.Terminal, 0, 0);
        var second = document.AddNode(NetNodeType.ROADM, 100, 0);

        document.AddLink(first, second, 25, LinkType.Directed);
        var twoWay = document.AddLink(second, first, 40, LinkType.Undirected);

        Assert.Equal(2, document.Links.Count);
        Assert.Equal(3, document.Network.Optical.EdgeCount);
        Assert.NotNull(twoWay.Link.Reverse);
        Assert.Equal(LinkType.Undirected, twoWay.Link.LinkType);
    }

    [Fact]
    public void UpdateLink_ReplacesReverseEdgeWhenDirectionChanges()
    {
        var document = new NetworkEditorViewModel();
        var first = document.AddNode(NetNodeType.Terminal, 0, 0);
        var second = document.AddNode(NetNodeType.ROADM, 100, 0);
        var edge = document.AddLink(first, second, 25, LinkType.Undirected);

        document.UpdateLink(edge, 55, LinkType.Directed);

        Assert.Equal(1, document.Network.Optical.EdgeCount);
        Assert.Equal(55, edge.Link.Tag);
        Assert.Null(edge.Link.Reverse);
        Assert.Same(first.Node, edge.Link.Source);
        Assert.Same(second.Node, edge.Link.Target);

        document.UpdateLink(edge, 70, LinkType.Undirected);
        Assert.Equal(2, document.Network.Optical.EdgeCount);
        Assert.NotNull(edge.Link.Reverse);
    }

    [Fact]
    public void RouteCommands_UseCurrentRequiredAndAvoidedSelections()
    {
        var document = new NetworkEditorViewModel();
        var source = document.AddNode(NetNodeType.Terminal, 0, 0);
        var required = document.AddNode(NetNodeType.OLA, 100, 0);
        var target = document.AddNode(NetNodeType.Terminal, 200, 0);
        document.AddLink(source, required, 10, LinkType.Undirected);
        document.AddLink(required, target, 10, LinkType.Undirected);
        document.AddLink(source, target, 30, LinkType.Undirected);
        document.FindOpticRoutes(source.Node.Id, target.Node.Id, 2);
        Assert.Equal(2, document.Routes.Count);
        Assert.Equal(20, document.Routes[0].Length);

        var graphRevision = document.Revision;
        document.SetRequiredRouteNode(required, true);
        Assert.Equal(graphRevision, document.Revision);
        Assert.Empty(document.Routes);
        document.FindOpticRoutes(source.Node.Id, target.Node.Id, 20);
        Assert.Single(document.Routes);
        Assert.Contains(document.Routes[0].Links, link =>
            ReferenceEquals(link.Source, required.Node) || ReferenceEquals(link.Target, required.Node));

        document.SetAvoidedRouteNode(required, true);
        Assert.False(document.IsRequiredRouteNode(required));
        Assert.True(document.IsAvoidedRouteNode(required));
        Assert.Empty(document.Routes);
        document.FindOpticRoutes(source.Node.Id, target.Node.Id, 2);
        Assert.Single(document.Routes);
        Assert.DoesNotContain(document.Routes[0].Links, link =>
            ReferenceEquals(link.Source, required.Node) || ReferenceEquals(link.Target, required.Node));
    }

    [Fact]
    public void RouteSearch_ReportsUnreachableEndpointsAndGraphChangesClearResults()
    {
        var document = new NetworkEditorViewModel();
        var source = document.AddNode(NetNodeType.Terminal, 0, 0);
        var target = document.AddNode(NetNodeType.Terminal, 100, 0);

        document.FindOpticRoutes(source.Node.Id, target.Node.Id, 2);
        Assert.Empty(document.Routes);
        Assert.NotNull(document.RouteError);

        document.AddLink(source, target, 12, LinkType.Directed);
        Assert.Null(document.RouteError);
        document.FindOpticRoutes(source.Node.Id, target.Node.Id, 1);
        Assert.Single(document.Routes);

        document.SetAvoidedRouteNode(target, true);
        Assert.Empty(document.Routes);
        Assert.Null(document.RouteError);
    }
}
