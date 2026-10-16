using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using OTN.Utils;

namespace OTN.Benchmarks;

internal static class BenchmarkNetworkInputs
{
    public static (Network Network, NetNode Source, NetNode Target) Create(
        int totalNodes,
        int backboneNodes,
        int connectionAttemptUpperBound,
        int mustPassNodes,
        int mustAvoidNodes,
        int seed)
    {
        var network = NetworkFactory.Create(totalNodes, backboneNodes, connectionAttemptUpperBound, mustPassNodes, mustAvoidNodes, seed);
        var graph = network.Optical;
        var eligibleNodes = graph.Vertices.Where(node => node.RoutingRole != RouteNodeType.OutRoute).ToArray();
        var targetNodes = eligibleNodes.Where(node => node.RoutingRole != RouteNodeType.InRoute).ToArray();
        var requiredNodes = graph.Vertices.Where(node => node.RoutingRole == RouteNodeType.InRoute).ToArray();

        foreach (var source in eligibleNodes)
        {
            var reachable = FindReachableNodes(graph, source);
            if (!requiredNodes.All(reachable.Contains))
                continue;

            var target = targetNodes.FirstOrDefault(node => node != source && reachable.Contains(node));
            if (target is not null)
                return (network, source, target);
        }

        throw new InvalidOperationException("The seeded benchmark network has no reachable source/target pair outside OutRoute nodes.");
    }

    private static HashSet<NetNode> FindReachableNodes(QuikGraph.IVertexAndEdgeListGraph<NetNode, Link> graph, NetNode source)
    {
        var reachable = new HashSet<NetNode> { source };
        var pending = new Queue<NetNode>();
        pending.Enqueue(source);

        while (pending.TryDequeue(out var current))
        {
            foreach (var edge in graph.OutEdges(current))
            {
                var next = edge.Target;
                if (next.RoutingRole != RouteNodeType.OutRoute && reachable.Add(next))
                    pending.Enqueue(next);
            }
        }

        return reachable;
    }
}
