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
    private NetNode _source = null!;
    private NetNode _target = null!;
    private List<Link> _bestRoute = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _network = NetworkFactory.Create(35, 5, 3);
        
        var rnd = new Random();
        var n = _network.Optical.Vertices.ToArray();
        _source = n[rnd.Next(n.Length)];
        _target = n[rnd.Next(n.Length)];

        var dijkstra = _network.Optical.ShortestPathsDijkstra(l => l.Tag, _source);
        IEnumerable<Link> r;
        while (_source.Equals(_target) || !dijkstra(_target, out r))
            _target = n[rnd.Next(n.Length)];

        _bestRoute = r.ToList();
    }

    [Test]
    public void FindOpticPathsAsync_AssertPathFound()
    {
        // Assert that without constraints our functions returns an obviously dijkstra route
        var kRoutes = _network.Optical.FindOpticPath(_source, _target);
        var mustPassRoute = _network.Optical.FindMustPassOpticPath(_source, _target);

        Assert.Multiple(() =>
        {
            Assert.That(_bestRoute.SequenceEqual(kRoutes[0]));
            Assert.That(_bestRoute.SequenceEqual(mustPassRoute));
        });
    }
    
    [Test]
    public void FindOpticPathsAsync_AssertPathUnique()
    {
        // Assert that k-paths are unique
        var routes = _network.Optical.FindOpticPath(_source, _target);
        var bestRoute = routes[0];

        Assert.That(_bestRoute.SequenceEqual(bestRoute));
        for (var i = 0; i < routes.Count - 1; i++)
        {
            var currentRoute = routes[i];
            Assert.That(routes.Skip(i + 1).All(r => !r.SequenceEqual(currentRoute)));
        }
    }
}