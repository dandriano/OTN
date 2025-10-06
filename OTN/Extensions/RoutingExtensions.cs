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
        if (link.Source.RoutingType == RouteNodeType.OutRoute || link.Target.RoutingType == RouteNodeType.OutRoute)
            return double.PositiveInfinity;

        return link.Tag;
    }

    public static List<List<Link>> FindOpticPaths(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source,
        NetNode target,
        int k = 5)
    {
        var dijkstra = graph.ShortestPathsDijkstra(CalculateLength, source);
        if (!dijkstra(target, out var path))
            return new List<List<Link>>();

        var result = new List<List<Link>>() { path!.ToList() };
        var candidates = new List<(double Cost, List<Link> Path)>();

        for (int i = 2; i <= k; i++)
        {
            var lastPath = result[^1];
            var lastNodes = GetNodesFromPath(lastPath).ToList();
            var candidateCount = candidates.Count;
            for (int spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                var spurNode = lastNodes[spurIdx];
                var rootNodes = lastNodes.GetRange(0, spurIdx + 1);
                var rootEdges = lastPath.Take(spurIdx).ToList();

                var removedEdges = new HashSet<Guid>();
                foreach (var p in result)
                {
                    var pNodes = GetNodesFromPath(p).ToList();
                    if (pNodes.Count > spurIdx + 1 && pNodes.GetRange(0, spurIdx + 1).SequenceEqual(rootNodes))
                        removedEdges.Add(p[spurIdx].Id);
                }

                Func<Link, double> weight = e => removedEdges.Contains(e.Id) ? float.PositiveInfinity : e.CalculateLength();

                if (!dijkstra(target, out var spurPath))
                    continue;
                
                candidates.Add((spurPath.Sum(p => p.CalculateLength()), spurPath.ToList()));
            }

            // Sort candidates (if changed)
            if (candidateCount != candidates.Count)
                candidates.Sort((a, b) => a.Cost.CompareTo(b.Cost));

            // Move shortest candidate to results (if any)
            if (candidates.Count != 0)
            {
                result.Add(candidates[0].Path);
                candidates.RemoveAt(0);
            }
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