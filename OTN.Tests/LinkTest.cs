using System;
using NUnit.Framework;
using OTN.Core;
using OTN.Enums;
using OTN.Interfaces;

namespace OTN.Tests;

[TestFixture]
public class LinkTest
{
    [Test]
    public void ReverseConstructor_CreatesCrossLinkedPair()
    {
        var nodeA = new NetNode(NetNodeType.Terminal);
        var nodeB = new NetNode(NetNodeType.ROADM);
        const double length = 42.5;

        var forward = new Link(nodeA, nodeB, length, LinkType.Undirected);
        var reverse = new Link(forward);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Source, Is.SameAs(nodeA));
            Assert.That(forward.Target, Is.SameAs(nodeB));
            Assert.That(forward.Tag, Is.EqualTo(length));
            Assert.That(forward.LinkType, Is.EqualTo(LinkType.Undirected));

            Assert.That(reverse.Source, Is.SameAs(nodeB));
            Assert.That(reverse.Target, Is.SameAs(nodeA));
            Assert.That(reverse.Tag, Is.EqualTo(length));
            Assert.That(reverse.LinkType, Is.EqualTo(LinkType.Undirected));

            Assert.That(forward.Reverse, Is.SameAs(reverse));
            Assert.That(reverse.Reverse, Is.SameAs(forward));
        });
    }

    [Test]
    public void ReverseConstructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Link((Link)null!));
    }

    [Test]
    public void ReverseConstructor_ThrowsOnDirectedLink()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);
        var directed = new Link(source, target, 15.0, LinkType.Directed);

        var ex = Assert.Throws<ArgumentException>(() => new Link(directed));
        Assert.That(ex!.Message, Does.Contain("directed link"));
    }

    [Test]
    public void ReverseConstructor_ThrowsIfReverseAlreadyExists()
    {
        var nodeA = new NetNode(NetNodeType.Terminal);
        var nodeB = new NetNode(NetNodeType.ROADM);

        var forward = new Link(nodeA, nodeB, 10.0, LinkType.Undirected);
        _ = new Link(forward);

        // Attempting to create a second reverse for the same forward link
        Assert.Throws<InvalidOperationException>(() => new Link(forward));
    }

    [Test]
    public void DirectedLink_HasNoReverse()
    {
        var source = new NetNode(NetNodeType.Terminal);
        var target = new NetNode(NetNodeType.OLA);

        var link = new Link(source, target, 15.0, LinkType.Directed);

        Assert.Multiple(() =>
        {
            Assert.That(link.LinkType, Is.EqualTo(LinkType.Directed));
            Assert.That(link.Reverse, Is.Null);
        });
    }

    [Test]
    public void Network_AddLink_AddsUndirectedPair()
    {
        var network = new Network();
        var nodeA = (NetNode)network.AddNetNode(NetNodeType.Terminal);
        var nodeB = (NetNode)network.AddNetNode(NetNodeType.ROADM);

        var forward = (Link)network.AddLink(nodeA, nodeB, 50.0, LinkType.Undirected);

        Assert.Multiple(() =>
        {
            Assert.That(network.Optical.ContainsEdge(forward), Is.True);
            Assert.That(forward.Reverse, Is.Not.Null);
            Assert.That(network.Optical.ContainsEdge(forward.Reverse!), Is.True);
            Assert.That(network.Optical.EdgeCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Network_AddLink_AddsSingleDirectedEdge()
    {
        var network = new Network();
        var nodeA = (NetNode)network.AddNetNode(NetNodeType.Terminal);
        var nodeB = (NetNode)network.AddNetNode(NetNodeType.ROADM);

        var directed = (Link)network.AddLink(nodeA, nodeB, 50.0, LinkType.Directed);

        Assert.Multiple(() =>
        {
            Assert.That(network.Optical.ContainsEdge(directed), Is.True);
            Assert.That(network.Optical.EdgeCount, Is.EqualTo(1));
            Assert.That(directed.Reverse, Is.Null);
        });
    }
}
