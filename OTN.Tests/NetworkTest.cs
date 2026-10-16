using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Extensions;
using OTN.Utils;
using QuikGraph.Algorithms;

namespace OTN.Tests;

public sealed class NetworkFixture
{
    public Network Network { get; }
    public NetNode Source { get; }
    public NetNode Target { get; private set; }
    public List<Link> BestRoute { get; }

    public NetworkFixture()
    {
        Network = NetworkFactory.Create(35, 5, 3);

        var rnd = new Random();
        var nodes = Network.Optical.Vertices.ToArray();
        Source = nodes[rnd.Next(nodes.Length)];
        Target = nodes[rnd.Next(nodes.Length)];

        var dijkstra = Network.Optical.ShortestPathsDijkstra(link => link.Tag, Source);
        IEnumerable<Link> route;
        while (Source.Equals(Target) || !dijkstra(Target, out route))
            Target = nodes[rnd.Next(nodes.Length)];

        BestRoute = route.ToList();
    }
}

public class NetworkTest : IClassFixture<NetworkFixture>
{
    private readonly NetworkFixture _fixture;

    public NetworkTest(NetworkFixture fixture) => _fixture = fixture;

    [Fact]
    public void Create_WithSameSeed_ProducesSameTopologyAndWeights()
    {
        var first = NetworkFactory.Create(35, 5, 3, mustPassNodes: 4, mustAvoidNodes: 5, seed: 421);
        var second = NetworkFactory.Create(35, 5, 3, mustPassNodes: 4, mustAvoidNodes: 5, seed: 421);

        var firstNodes = first.Optical.Vertices.ToArray();
        var secondNodes = second.Optical.Vertices.ToArray();
        var firstNodeIndices = firstNodes.Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);
        var secondNodeIndices = secondNodes.Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);

        Assert.Equal(
            firstNodes.Select(node => (node.Type, node.RoutingRole)),
            secondNodes.Select(node => (node.Type, node.RoutingRole)));

        var firstEdges = first.Optical.Edges
            .Select(edge => (Source: firstNodeIndices[edge.Source], Target: firstNodeIndices[edge.Target], Weight: edge.Tag))
            .OrderBy(edge => edge.Source)
            .ThenBy(edge => edge.Target)
            .ThenBy(edge => edge.Weight)
            .ToArray();
        var secondEdges = second.Optical.Edges
            .Select(edge => (Source: secondNodeIndices[edge.Source], Target: secondNodeIndices[edge.Target], Weight: edge.Tag))
            .OrderBy(edge => edge.Source)
            .ThenBy(edge => edge.Target)
            .ThenBy(edge => edge.Weight)
            .ToArray();

        Assert.Equal(firstEdges, secondEdges);
    }

    [Fact]
    public void FindOpticPathsAsync_AssertPathFound()
    {
        // Assert that without constraints our functions returns an obviously dijkstra route
        var kRoutes = _fixture.Network.Optical.FindOpticPath(
            _fixture.Source, _fixture.Target, cancellationToken: TestContext.Current.CancellationToken);
        var mustPassRoute = _fixture.Network.Optical.FindMustPassOpticPath(_fixture.Source, _fixture.Target);

        Assert.Multiple(() =>
        {
            Assert.Equal(_fixture.BestRoute, kRoutes[0]);
            Assert.Equal(_fixture.BestRoute, mustPassRoute);
        });
    }

    [Fact]
    public void FindOpticPathsAsync_AssertPathUnique()
    {
        // Assert that k-paths are unique
        var routes = _fixture.Network.Optical.FindOpticPath(
            _fixture.Source, _fixture.Target, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEmpty(routes);

        if (routes.Count == 1)
            return;

        for (var i = 0; i < routes.Count - 1; i++)
        {
            var currentRoute = routes[i];
            Assert.True(routes.Skip(i + 1).All(route =>
                !route.SequenceEqual(_fixture.BestRoute) && !route.SequenceEqual(currentRoute)));
        }
    }
}
