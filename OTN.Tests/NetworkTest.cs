using System;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using OTN.Extensions;
using OTN.Utils;
using QuikGraph;

namespace OTN.Tests;

public class NetworkTest
{
    [Fact]
    public void Create_WithSameSeed_ProducesSameTopologyAndWeights()
    {
        var first = NetworkFactory.Create(35, 5, 3, seed: 421);
        var second = NetworkFactory.Create(35, 5, 3, seed: 421);
        var firstNodes = first.Optical.Vertices.ToArray();
        var secondNodes = second.Optical.Vertices.ToArray();
        var firstIndices = firstNodes.Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);
        var secondIndices = secondNodes.Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);

        Assert.Equal(firstNodes.Select(node => node.Type), secondNodes.Select(node => node.Type));
        var firstEdges = first.Optical.Edges.Select(edge => (Source: firstIndices[edge.Source], Target: firstIndices[edge.Target], Weight: edge.Tag))
            .OrderBy(edge => edge.Source).ThenBy(edge => edge.Target).ThenBy(edge => edge.Weight).ToArray();
        var secondEdges = second.Optical.Edges.Select(edge => (Source: secondIndices[edge.Source], Target: secondIndices[edge.Target], Weight: edge.Tag))
            .OrderBy(edge => edge.Source).ThenBy(edge => edge.Target).ThenBy(edge => edge.Weight).ToArray();
        Assert.Equal(firstEdges, secondEdges);
    }

    [Fact]
    public void RequiredUnreachableAndAvoidedEndpointsFailClearly()
    {
        var source = new NetNode(NetNodeType.ROADM);
        var target = new NetNode(NetNodeType.ROADM);
        var required = new NetNode(NetNodeType.ROADM);
        var graph = new BidirectionalGraph<NetNode, Link>();
        graph.AddVertexRange([source, target, required]);
        graph.AddVerticesAndEdge(new Link(source, target, 1));

        var cancellationToken = TestContext.Current.CancellationToken;
        var unreachable = Assert.Throws<NoPathFoundException>(() => graph.FindOpticPath(source, target, constraints: new RouteConstraints([required]), cancellationToken: cancellationToken));
        var avoidedEndpoint = Assert.Throws<NoPathFoundException>(() => graph.FindOpticPath(source, target, constraints: new RouteConstraints(avoidedNodes: [target]), cancellationToken: cancellationToken));

        Assert.Contains("required node", unreachable.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("avoided", avoidedEndpoint.Message, StringComparison.OrdinalIgnoreCase);
    }
}
