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
            Assert.Equal(LinkType.Duplex, forward.LinkType);

            Assert.Same(nodeB, reverse.Source);
            Assert.Same(nodeA, reverse.Target);
            Assert.Equal(length, reverse.Tag);
            Assert.Equal(LinkType.Duplex, reverse.LinkType);

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
    public void ReverseConstructor_ThrowsOnSimplexLink()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);
        var simplex = new Link(source, target, 15.0, LinkType.Simplex);

        var ex = Assert.Throws<ArgumentException>(() => new Link(simplex));
        Assert.Contains("simplex link", ex!.Message);
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
    public void SimplexLink_HasNoReverse()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);

        var link = new Link(source, target, 15.0, LinkType.Simplex);

        Assert.Multiple(() =>
        {
            Assert.Equal(LinkType.Simplex, link.LinkType);
            Assert.Null(link.Reverse);
        });
    }

    [Fact]
    public void Network_AddLink_AddsDuplexPair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);

        var forward = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Duplex);

        Assert.Multiple(() =>
        {
            Assert.True(network.Optical.ContainsEdge(forward));
            Assert.NotNull(forward.Reverse);
            Assert.True(network.Optical.ContainsEdge(forward.Reverse!));
            Assert.Equal(2, network.Optical.EdgeCount);
        });
    }

    [Fact]
    public void Network_AddLink_AddsSingleSimplexEdge()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);

        var simplex = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Simplex);

        Assert.Multiple(() =>
        {
            Assert.True(network.Optical.ContainsEdge(simplex));
            Assert.Equal(1, network.Optical.EdgeCount);
            Assert.Null(simplex.Reverse);
        });
    }

    [Fact]
    public void Network_RemoveLink_RemovesDuplexPair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.Add(NetNodeType.Terminal);
        var nodeB = (NetNode)network.Add(NetNodeType.ROADM);
        var forward = (Link)network.Add(nodeA, nodeB, 50.0, LinkType.Duplex);

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
        network.Add(nodeA, nodeB, 50.0, LinkType.Duplex);
        network.Add(nodeB, nodeC, 25.0, LinkType.Simplex);

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
        var original = network.Add(nodeA, nodeB, 50.0, LinkType.Duplex);

        var updated = network.Update(original, 25.0, LinkType.Simplex);

        Assert.DoesNotContain(network.Optical.Edges, edge => ReferenceEquals(edge, original));
        Assert.DoesNotContain(network.Optical.Edges, edge => ReferenceEquals(edge, original.Reverse));
        Assert.Contains(network.Optical.Edges, edge => ReferenceEquals(edge, updated));
        Assert.Equal(1, network.Optical.EdgeCount);
        Assert.Equal(25.0, updated.Tag);
        Assert.Equal(LinkType.Simplex, updated.LinkType);
    }
}
