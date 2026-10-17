using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OTN.Core;
using QuikGraph;
using QuikGraph.Algorithms;

namespace OTN.Extensions;

public static class RoutingExtensions
{
    private sealed class RoutingWeight(RouteConstraints constraints, HashSet<Guid>? blockedEdges = null, HashSet<Guid>? blockedNodes = null)
    {
        public double GetWeight(Link edge)
        {
            if (constraints.AvoidedNodes.Contains(edge.Source) || constraints.AvoidedNodes.Contains(edge.Target) ||
                (blockedEdges?.Contains(edge.Id) ?? false) ||
                (blockedNodes?.Contains(edge.Source.Id) ?? false) || (blockedNodes?.Contains(edge.Target.Id) ?? false))
                return double.PositiveInfinity;
            return edge.Tag;
        }
    }

    private static IEnumerable<NetNode> GetNodesFromPath(this IEnumerable<Link> path)
    {
        using var enumerator = path.GetEnumerator();
        if (!enumerator.MoveNext()) 
            yield break;

        yield return enumerator.Current.Source;

        do 
        { 
            yield return enumerator.Current.Target;
        }
        while (enumerator.MoveNext());
    }

    /// <summary>
    /// Finds up to <paramref name="k"/> shortest paths from a source node to a target node in an optical network graph.
    /// </summary>
    /// <param name="graph">The graph representing the network, implementing <see cref="IVertexAndEdgeListGraph{NetNode, Link}"/>.</param>
    /// <param name="source">The starting node for the paths.</param>
    /// <param name="target">The destination node for the paths.</param>
    /// <param name="k">The number of shortest paths to find.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
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

        var constraints = RouteConstraints.Empty;
        return FindKShortestPaths(graph, source, target, k, constraints, cancellationToken);
    }

    /// <summary>
    /// Finds one route through required nodes, or up to <paramref name="k"/> shortest paths when none are required.
    /// Avoided nodes are excluded in either case.
    /// </summary>
    /// <param name="graph">The graph representing the optical network.</param>
    /// <param name="source">The starting node for the paths.</param>
    /// <param name="target">The destination node for the paths.</param>
    /// <param name="constraints">Required and avoided nodes for this search.</param>
    /// <param name="k">The maximum number of paths when no nodes are required.</param>
    /// <param name="cancellationToken">A token used to cancel the search.</param>
    /// <returns>A list of paths, each represented by its ordered links.</returns>
    /// <exception cref="ArgumentException">A constraint refers to a node outside the graph.</exception>
    /// <exception cref="NoPathFoundException">The constraints cannot be satisfied by a route.</exception>
    public static List<List<Link>> FindOpticPath(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        RouteConstraints constraints,
        int k = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (constraints.AvoidedNodes.Contains(source) || constraints.AvoidedNodes.Contains(target))
            throw new NoPathFoundException("The source or target is avoided, so no route can be found.");

        if (constraints.RequiredNodes.Count > 0)
            return [FindRequiredNodePath(graph, source, target, constraints, cancellationToken)];

        return FindKShortestPaths(graph, source, target, k, constraints, cancellationToken);
    }

    private static List<List<Link>> FindKShortestPaths(
        IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        int k,
        RouteConstraints constraints,
        CancellationToken cancellationToken)
    {
        if (constraints.AvoidedNodes.Contains(source) || constraints.AvoidedNodes.Contains(target))
            throw new NoPathFoundException("The source or target is avoided, so no route can be found.");

        var initialWeight = new RoutingWeight(constraints);
        var dijkstra = graph.ShortestPathsDijkstra(initialWeight.GetWeight, source);
        cancellationToken.ThrowIfCancellationRequested();
        if (!dijkstra(target, out var initialPath))
            throw new NoPathFoundException($"No path between {source}/{target}");

        var result = new List<List<Link>> { initialPath!.ToList() };
        var resultNodes = new List<List<NetNode>> { GetNodesFromPath(result[0]).ToList() };
        
        var signature = new StringBuilder();
        foreach (var node in resultNodes[0])
        {
            if (signature.Length > 0) 
                signature.Append('>');
            signature.Append(node.Id);
        }
        
        var signatures = new HashSet<string> { signature.ToString()};
        var candidates = new PriorityQueue<List<Link>, double>();

        for (var i = 2; i <= k; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lastPath = result[^1];
            var lastNodes = resultNodes[^1];

            for (var spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var spurNode = lastNodes[spurIdx];
                var rootNodes = lastNodes.GetRange(0, spurIdx + 1);
                var fullPath = new List<Link>();
                var costRoot = 0d;
                foreach (var edge in lastPath.Take(spurIdx))
                {
                    fullPath.Add(edge);
                    costRoot += edge.Tag;
                }

                var removedEdges = new HashSet<Guid>();
                for (var rIdx = 0; rIdx < result.Count; rIdx++)
                {
                    var pathNodes = resultNodes[rIdx];
                    if (pathNodes.Count > spurIdx + 1 && pathNodes.GetRange(0, spurIdx + 1).SequenceEqual(rootNodes))
                        removedEdges.Add(result[rIdx][spurIdx].Id);
                }

                var blockedNodes = new HashSet<Guid>();
                for (var n = 0; n < spurIdx; n++) 
                    blockedNodes.Add(lastNodes[n].Id);

                var spurWeight = new RoutingWeight(constraints, removedEdges, blockedNodes);
                var spurDijkstra = graph.ShortestPathsDijkstra(spurWeight.GetWeight, spurNode);
                if (!spurDijkstra(target, out var spurPath)) 
                    continue;

                signature = new StringBuilder();
                for (var index = 0; index < rootNodes.Count; index++)
                {
                    if (index > 0) signature.Append('>');
                    signature.Append(rootNodes[index].Id);
                }
                var cost = costRoot;
                foreach (var edge in spurPath!)
                {
                    fullPath.Add(edge);
                    cost += edge.Tag;
                    signature.Append('>').Append(edge.Target.Id);
                }
                if (signatures.Add(signature.ToString())) 
                    candidates.Enqueue(fullPath, cost);
            }

            if (candidates.Count == 0) break;
            var nextPath = candidates.Dequeue();
            result.Add(nextPath);
            resultNodes.Add(GetNodesFromPath(nextPath).ToList());
        }
        return result;
    }

    private static List<Link> FindRequiredNodePath(
        IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        RouteConstraints constraints,
        CancellationToken cancellationToken)
    {
        var remaining = new HashSet<NetNode>(constraints.RequiredNodes);
        remaining.Remove(source);
        remaining.Remove(target);
        var current = source;
        var route = new List<Link>();

        while (remaining.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var weight = new RoutingWeight(constraints);
            var dijkstra = graph.ShortestPathsDijkstra(weight.GetWeight, current);
            double? bestDistance = null;
            NetNode? nearest = null;
            List<Link>? nearestPath = null;
            foreach (var node in remaining)
            {
                if (!dijkstra(node, out var path)) 
                    continue;

                var candidatePath = path!.ToList();
                var distance = candidatePath.Sum(weight.GetWeight);
                if (bestDistance is null || distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = node;
                    nearestPath = candidatePath;
                }
            }
            if (nearest is null)
                throw new NoPathFoundException($"A required node cannot be reached from {current} while avoiding the selected nodes.");

            route.AddRange(nearestPath!);
            foreach (var visitedNode in GetNodesFromPath(nearestPath!))
                remaining.Remove(visitedNode);
            remaining.Remove(nearest);
            current = nearest;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var finalWeight = new RoutingWeight(constraints);
        if (!graph.ShortestPathsDijkstra(finalWeight.GetWeight, current)(target, out var finalPath))
            throw new NoPathFoundException($"No path between {current}/{target} while visiting the required nodes.");
        route.AddRange(finalPath!);
        return route;
    }
}
