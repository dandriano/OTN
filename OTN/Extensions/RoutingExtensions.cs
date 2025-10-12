using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using QuikGraph;
using QuikGraph.Algorithms;

namespace OTN.Extensions;

public static class RoutingExtensions
{
    public static double CalculateLength(this Link link)
    {
        if (link.Source.RoutingRole == RouteNodeType.OutRoute || link.Target.RoutingRole == RouteNodeType.OutRoute)
            return double.PositiveInfinity;

        return link.Tag;
    }

    /// <summary>
    /// Finds up to <paramref name="k"/> shortest paths from a source node to a target node in an optical network graph.
    /// </summary>
    /// <param name="graph">The graph representing the network, implementing <see cref="IVertexAndEdgeListGraph{NetNode, Link}"/>.</param>
    /// <param name="source">The starting node for the paths.</param>
    /// <param name="target">The destination node for the paths.</param>
    /// <param name="k">The number of shortest paths to find. Default is 5.</param>
    /// <returns>
    /// A list of paths, where each path is a list of <see cref="Link"/> objects representing edges in the path.
    /// If no path is found, returns an empty list.
    /// </returns>
    /// <remarks>
    /// This method uses a variation of Yen's k-shortest paths algorithm.
    /// It repeatedly finds shortest paths by temporarily removing edges from previously found paths to discover alternative routes.
    /// Internally, it leverages Dijkstra's algorithm to find the shortest paths on the modified graph.
    /// </remarks>
    public static List<List<Link>> FindOpticPath(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        int k = 5)
    {
        var dijkstra = graph.ShortestPathsDijkstra(CalculateLength, source);
        if (!dijkstra(target, out var path))
            throw new NoPathFoundException($"No path between {source}/{target}");

        var result = new List<List<Link>>() { path!.ToList() };
        var candidates = new PriorityQueue<List<Link>, double>();

        for (int i = 2; i <= k; i++)
        {
            var lastPath = result[^1];
            var lastNodes = GetNodesFromPath(lastPath).ToList();

            for (int spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                var spurNode = lastNodes[spurIdx];
                var rootNodes = lastNodes.GetRange(0, spurIdx + 1);
                var rootEdges = lastPath.Take(spurIdx).ToList();

                // Identify deviation edges from previous paths sharing the prefix
                var removedEdges = new HashSet<Guid>();
                foreach (var p in result)
                {
                    var pNodes = GetNodesFromPath(p).ToList();
                    if (pNodes.Count > spurIdx + 1 && pNodes.GetRange(0, spurIdx + 1).SequenceEqual(rootNodes))
                        removedEdges.Add(p[spurIdx].Id);
                }

                // Temporarily set prefix nodes (except spurNode) to OutRoute to prevent using them in spur path
                var tempOutNodes = new List<NetNode>();
                for (int j = 0; j < spurIdx; j++)
                {
                    var node = lastNodes[j];
                    if (node.RoutingRole != RouteNodeType.OutRoute)
                    {
                        node.SetRouteRole(RouteNodeType.OutRoute);
                        tempOutNodes.Add(node);
                    }
                }

                try
                {
                    Func<Link, double> weight = e => removedEdges.Contains(e.Id) ? double.PositiveInfinity : e.CalculateLength();

                    // Run Dijkstra from spurNode to target
                    dijkstra = graph.ShortestPathsDijkstra(weight, spurNode);
                    if (!dijkstra(target, out var spurPath))
                        continue;

                    var fullPath = rootEdges.Concat(spurPath!).ToList();
                    var cost = fullPath.Sum(e => e.Tag);
                    candidates.Enqueue(fullPath, cost);
                }
                finally
                {
                    foreach (var node in tempOutNodes)
                        node.SetRouteRole(RouteNodeType.Undefined);
                }
            }

            // If no candidates, cannot find more paths
            if (candidates.Count == 0)
                break;

            // Add the shortest candidate to result
            var nextPath = candidates.Dequeue();
            result.Add(nextPath);
        }

        return result;
    }
    
    /// <summary>
    /// Using the Nearest Neighbor heuristic finds path thru the must pass nodes
    /// </summary>
    /// <param name="graph">The graph representing nodes as <see cref="NetNode"/> and edges as <see cref="Link"/> with weights.</param>
    /// <param name="source">The starting vertex for the "TSP tour".</param>
    /// <returns>
    /// A list containing a single path, which is a list of <see cref="Link"/> edges representing the tour through the graph.
    /// The tour starts and ends at the <paramref name="source"/> node.
    /// </returns>
    /// <remarks>
    /// The heuristic constructs a tour by repeatedly visiting the nearest unvisited neighbor node.
    /// The resulting tour is returned as a sequence of edges (links) consistent with the graph representation.
    /// </remarks>
    public static List<Link> FindMustPassOpticPath(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target)
    {
        // Restrict to route-related nodes
        var routeNodes = new HashSet<NetNode>(
            graph.Vertices.Where(v => v.RoutingRole == RouteNodeType.InRoute));

        var visited = new HashSet<NetNode> { source };
        var nodes = new List<NetNode> { source };
        var current = source;

        // Build weights function for Dijkstra on Links
        Func<Link, double> weight = e => e.CalculateLength();

        // Precompute shortest paths from 'current' to all route nodes using Dijkstra to limit search space
        var dijkstra = graph.ShortestPathsDijkstra(weight, current);

        while (visited.Count < routeNodes.Count)
        {
            // Find nearest unvisited route node reachable from current
            double? minDistance = null;
            NetNode? nearest = null;

            foreach (var node in routeNodes)
            {
                if (visited.Contains(node))
                    continue;

                if (dijkstra(node, out var path))
                {
                    var dist = path.Sum(e => e.CalculateLength());
                    if (minDistance == null || dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = node;
                    }
                }
            }

            if (nearest == null)
                break;

            visited.Add(nearest);
            nodes.Add(nearest);
            current = nearest;

            dijkstra = graph.ShortestPathsDijkstra(weight, current);
        }

        nodes.Add(target); // complete the loop

        // Convert vertex path to edges by shortest paths between consecutive vertices:
        var result = new List<Link>();
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var from = nodes[i];
            var to = nodes[i + 1];

            if (!graph.ShortestPathsDijkstra(weight, from)(to, out var pathEdges))
                throw new NoPathFoundException($"No path between {source}/{target}");

            result.AddRange(pathEdges);
        }

        return result;
    }

    public static IEnumerable<NetNode> GetNodesFromPath(IEnumerable<Link> path)
    {
        using var enumerator = path.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        // Yield the first edge
        yield return enumerator.Current.Source;

        do
        {
            yield return enumerator.Current.Target;
        }
        while (enumerator.MoveNext());
    }
}