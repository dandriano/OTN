using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Utils;

namespace OTN.Benchmarks;

internal static class BenchmarkNetworkInputs
{
    public static (Network Network, NetNode Source, NetNode Target, RouteConstraints Constraints) Create(
        int totalNodes,
        int backboneNodes,
        int connectionAttemptUpperBound,
        int requiredNodeCount,
        int avoidedNodeCount,
        int seed)
    {
        if (requiredNodeCount < 0 || avoidedNodeCount < 0 || requiredNodeCount + avoidedNodeCount > totalNodes)
            throw new ArgumentOutOfRangeException(nameof(requiredNodeCount), "Constraint counts must fit within the network.");

        var network = NetworkFactory.Create(totalNodes, backboneNodes, connectionAttemptUpperBound, seed);
        var nodes = network.Optical.Vertices.ToArray();
        var required = nodes.Take(requiredNodeCount).ToArray();
        var avoided = nodes.Skip(totalNodes - avoidedNodeCount).ToArray();
        var constraints = new RouteConstraints(required, avoided);

        foreach (var source in nodes.Where(node => !constraints.AvoidedNodes.Contains(node)))
        {
            var reachable = FindReachableNodes(network.Optical, source, constraints.AvoidedNodes);
            if (!required.All(reachable.Contains)) 
                continue;
            var target = nodes.FirstOrDefault(node => node != source && !constraints.AvoidedNodes.Contains(node) && reachable.Contains(node));
            if (target is not null) 
                return (network, source, target, constraints);
        }

        throw new InvalidOperationException("The seeded benchmark network has no reachable source/target pair satisfying its route constraints.");
    }

    private static HashSet<NetNode> FindReachableNodes(
        QuikGraph.IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        IReadOnlySet<NetNode> avoided)
    {
        var reachable = new HashSet<NetNode> { source };
        var pending = new Queue<NetNode>();
        pending.Enqueue(source);
        while (pending.TryDequeue(out var current))
        {
            foreach (var edge in graph.OutEdges(current))
            {
                if (!avoided.Contains(edge.Target) && reachable.Add(edge.Target)) pending.Enqueue(edge.Target);
            }
        }
        return reachable;
    }
}
