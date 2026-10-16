using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OTN.Core;
using OTN.Enums;
using QuikGraph;
using QuikGraph.Algorithms;

namespace OTN.Extensions;

public static class RoutingExtensions
{
    private static IEnumerable<NetNode> GetNodesFromPath(this IEnumerable<Link> path)
    {
        using var enumerator = path.GetEnumerator();
        if (!enumerator.MoveNext()) 
            yield break;

        yield return enumerator.Current.Source;

        do
        {
            yield return enumerator.Current.Target;
        } while (enumerator.MoveNext());
    }

    private static double CalculateLength(this Link link)
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
    /// <param name="k">The number of shortest paths to find.</param>
    /// <returns>
    /// A list of paths, where each path is a list of <see cref="Link"/> objects representing edges in the path.
    /// </returns>
    /// <exception cref="NoPathFoundException">No paths found</exception>
    public static List<List<Link>> FindOpticPath(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        int k = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        // Initial Dijkstra from source
        var dijkstra = graph.ShortestPathsDijkstra(CalculateLength, source);
        cancellationToken.ThrowIfCancellationRequested();

        if (!dijkstra(target, out var initialPath))
            throw new NoPathFoundException($"No path between {source}/{target}");

        // Store initial path and its nodes
        var result = new List<List<Link>>() { initialPath!.ToList() };
        var resultNodes = new List<List<NetNode>>() { result[0].GetNodesFromPath().ToList() };

        // Signatures to avoid duplicates (path-node sequence)
        var signatures = new HashSet<string>
        {
            string.Join(">", resultNodes[0].Select(n => n.Id))
        };

        // Candidate paths (shortest first)
        var candidates = new PriorityQueue<List<Link>, double>();

        // Seed with the signature of the initial path
        // (We already added initial path, so start loop from i=2)
        for (var i = 2; i <= k; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lastPath = result[^1];
            var lastNodes = resultNodes[^1];

            // Spur loop
            for (var spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var spurNode = lastNodes[spurIdx];
                var rootNodes = lastNodes.GetRange(0, spurIdx + 1);

                var fullPath = new List<Link>();
                double costRoot = 0d;
                foreach (var e in lastPath.Take(spurIdx))
                {
                    fullPath.Add(e);
                    costRoot += e.Tag;
                }

                // Edges to remove to avoid replicating previous root path (same root)
                var removedEdges = new HashSet<Guid>();
                for (var rIdx = 0; rIdx < result.Count; rIdx++)
                {
                    var pNodes = resultNodes[rIdx];
                    if (pNodes.Count > spurIdx + 1 && pNodes.GetRange(0, spurIdx + 1).SequenceEqual(rootNodes))
                        removedEdges.Add(result[rIdx][spurIdx].Id);
                }

                // Block edges that either are in removedEdges or touch any node in rootNodes except the spurNode
                var blockedNodes = new HashSet<NetNode>();
                for (int n = 0; n < spurIdx; n++) 
                    blockedNodes.Add(lastNodes[n]);

                Func<Link, double> weight = e =>
                {
                    if (removedEdges.Contains(e.Id)) 
                        return double.PositiveInfinity;

                    if (blockedNodes.Contains(e.Source) || blockedNodes.Contains(e.Target)) 
                        return double.PositiveInfinity;

                    return e.CalculateLength();
                };

                // Run Dijkstra from spurNode to target using the filtered graph
                var spurDijkstra = graph.ShortestPathsDijkstra(weight, spurNode);
                if (!spurDijkstra(target, out var spurPath))
                    continue;

                // Build full path (rootEdges + spurPath), cost, and signature in one pass
                double cost = costRoot;
                var sb = new StringBuilder();
                // Root path signature: IDs of rootNodes
                for (int idx = 0; idx < rootNodes.Count; idx++)
                {
                    if (idx > 0) 
                        sb.Append('>');
                    sb.Append(rootNodes[idx].Id);
                }

                var nextNodes = new List<NetNode>(rootNodes.Count);
                nextNodes.AddRange(rootNodes);

                // Enumerate spurPath once to fill fullPath, cost, nextNodes, and signature
                int edgeCounter = 0;
                foreach (var edge in spurPath!)
                {
                    fullPath.Add(edge);
                    cost += edge.Tag;

                    sb.Append('>');
                    sb.Append(edge.Target.Id);
                    nextNodes.Add(edge.Target);

                    edgeCounter++;
                }

                if (signatures.Add(sb.ToString()))
                    candidates.Enqueue(fullPath, cost);
            }

            // If no candidates, cannot find more paths
            if (candidates.Count == 0)
                break;

            // Add the shortest candidate to result and keep its node sequence
            var nextPath = candidates.Dequeue();
            result.Add(nextPath);

            // Build nodes for the new path without re-walking the path
            var nextNodesFromPath = nextPath.GetNodesFromPath().ToList();
            resultNodes.Add(nextNodesFromPath);
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
}