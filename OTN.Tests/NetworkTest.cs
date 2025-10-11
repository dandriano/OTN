using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Extensions;
using OTN.Utils;
using QuikGraph.Algorithms;

namespace OTN.Tests;

[TestFixture]
public class NetworkTest
{
    private Network _network;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _network = NetworkFactory.Create(35, 5, 3);
    }

    [Test]
    public void FindOpticPathsAsync_AssertPathFound()
    {
        var rnd = new Random();
        var n = _network.Optical.Vertices.ToArray();
        var source = n[rnd.Next(n.Length)];
        var target = n[rnd.Next(n.Length)];

        var dijkstra = _network.Optical.ShortestPathsDijkstra(l => l.Tag, source);
        IEnumerable<Link> r;
        while (source.Equals(target) || !dijkstra(target, out r))
            target = n[rnd.Next(n.Length)];

        var targetRoute = r.ToList();
        var route1 = _network.Optical.FindOpticPath(source, target);
        var route2 = _network.Optical.FindMustPassOpticPath(source, target);

        Assert.Multiple(() =>
        {
            Assert.That(targetRoute.SequenceEqual(route1[0]));
            Assert.That(targetRoute.SequenceEqual(route2));
        });
    }
    
    [Test]
    public void FindOpticPathsAsync_AssertPathUniq()
    {
        var rnd = new Random();
        var n = _network.Optical.Vertices.ToArray();
        var source = n[rnd.Next(n.Length)];
        var target =  n[rnd.Next(n.Length)];

        var dijkstra = _network.Optical.ShortestPathsDijkstra(l => l.Tag, source);
        IEnumerable<Link> r;
        while(source.Equals(target) || !dijkstra(target, out r))
            target =  n[rnd.Next(n.Length)];

        var targetRoute = r.ToList();
        var routes = _network.Optical.FindOpticPath(source, target);
        var bestRoute = routes[0];

        Assert.That(targetRoute.SequenceEqual(bestRoute));
        Assert.That(routes.Skip(1).All(r => !r.SequenceEqual(bestRoute)));

        var uniqNodes = new Dictionary<Guid, NetNode>();
        foreach (var route in routes)
        {
            foreach (var node in RoutingExtensions.GetNodesFromPath(route)
                        .Where(n => n.Id != source.Id && n.Id != target.Id))
            {
                Assert.That(uniqNodes.ContainsKey(node.Id));
                uniqNodes.Add(node.Id, node);
            }
        }
    }
}