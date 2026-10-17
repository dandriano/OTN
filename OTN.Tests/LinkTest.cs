using System;
using OTN.Core;
using OTN.Enums;

namespace OTN.Tests;

public class LinkTest
{
    [Fact]
    public void ReverseConstructor_CreatesCrossLinkedPair()
    {
        var nodeA = new NetNode(NetNodeType.Terminal);
        var nodeB = new NetNode(NetNodeType.ROADM);
        const double length = 42.5;

        var forward = new Link(nodeA, nodeB, length);
        var reverse = new Link(forward);

        Assert.Multiple(() =>
        {
            Assert.Same(nodeA, forward.Source);
            Assert.Same(nodeB, forward.Target);
            Assert.Equal(length, forward.Tag);
            Assert.Equal(LinkType.Undirected, forward.LinkType);

            Assert.Same(nodeB, reverse.Source);
            Assert.Same(nodeA, reverse.Target);
            Assert.Equal(length, reverse.Tag);
            Assert.Equal(LinkType.Undirected, reverse.LinkType);

            Assert.Same(reverse, forward.Reverse);
            Assert.Same(forward, reverse.Reverse);
        });
    }

    [Fact]
    public void ReverseConstructor_ThrowsOnNull()
    {
        Assert.Throws<NullReferenceException>(() => new Link((Link)null!));
    }

    [Fact]
    public void ReverseConstructor_ThrowsOnDirectedLink()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);
        var directed = new Link(source, target, 15.0, LinkType.Directed);

        var ex = Assert.Throws<ArgumentException>(() => new Link(directed));
        Assert.Contains("directed link", ex!.Message);
    }

    [Fact]
    public void ReverseConstructor_ThrowsIfReverseAlreadyExists()
    {
        var nodeA = new NetNode(NetNodeType.Terminal);
        var nodeB = new NetNode(NetNodeType.ROADM);

        var forward = new Link(nodeA, nodeB, 10.0);
        _ = new Link(forward);

        // Attempting to create a second reverse for the same forward link
        Assert.Throws<InvalidOperationException>(() => new Link(forward));
    }

    [Fact]
    public void DirectedLink_HasNoReverse()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);

        var link = new Link(source, target, 15.0, LinkType.Directed);

        Assert.Multiple(() =>
        {
            Assert.Equal(LinkType.Directed, link.LinkType);
            Assert.Null(link.Reverse);
        });
    }

    [Fact]
    public void Network_AddLink_AddsUndirectedPair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);

        var forward = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Undirected);

        Assert.Multiple(() =>
        {
            Assert.True(network.Optical.ContainsEdge(forward));
            Assert.NotNull(forward.Reverse);
            Assert.True(network.Optical.ContainsEdge(forward.Reverse!));
            Assert.Equal(2, network.Optical.EdgeCount);
        });
    }

    [Fact]
    public void Network_AddLink_AddsSingleDirectedEdge()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);

        var directed = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Directed);

        Assert.Multiple(() =>
        {
            Assert.True(network.Optical.ContainsEdge(directed));
            Assert.Equal(1, network.Optical.EdgeCount);
            Assert.Null(directed.Reverse);
        });
    }

    [Fact]
    public void Network_RemoveLink_RemovesUndirectedPair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);
        var forward = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Undirected);

        Assert.True(network.Remove(forward));
        Assert.False(network.Optical.ContainsEdge(forward));
        Assert.False(network.Optical.ContainsEdge(forward.Reverse!));
        Assert.Equal(0, network.Optical.EdgeCount);
        Assert.False(network.Remove(forward));
    }

    [Fact]
    public void Network_RemoveNetNode_RemovesIncidentLinks()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);
        var nodeC = (NetNode)network.Add(NetNodeType.OLA);
        network.Add(nodeA, nodeB, 50.0, LinkType.Undirected);
        network.Add(nodeB, nodeC, 25.0, LinkType.Directed);

        Assert.True(network.Remove(nodeB));
        Assert.False(network.Optical.ContainsVertex(nodeB));
        Assert.Equal(0, network.Optical.EdgeCount);
        Assert.False(network.Remove(nodeB));
    }

    [Fact]
    public void Network_UpdateLink_ReplacesTheWholePair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);
        var original = network.Add(nodeA, nodeB, 50.0, LinkType.Undirected);

        var updated = network.Update(original, 25.0, LinkType.Directed);

        Assert.DoesNotContain(network.Optical.Edges, edge => ReferenceEquals(edge, original));
        Assert.DoesNotContain(network.Optical.Edges, edge => ReferenceEquals(edge, original.Reverse));
        Assert.Contains(network.Optical.Edges, edge => ReferenceEquals(edge, updated));
        Assert.Equal(1, network.Optical.EdgeCount);
        Assert.Equal(25.0, updated.Tag);
        Assert.Equal(LinkType.Directed, updated.LinkType);
    }
}
