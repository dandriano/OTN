using OTN.Enums;
using OTN.Interfaces;
using QuikGraph;
using QuikGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace OTN.Routing;

public static class RoutingExtensions
{
    public static double CalculateLength<TLink>(this TLink link)
        where TLink : ILink
    {
        if (link.Source.RoutingType == RouteNodeType.InRoute || link.Target.RoutingType == RouteNodeType.InRoute)
            return 0;

        if (link.Source.RoutingType == RouteNodeType.OutRoute || link.Target.RoutingType == RouteNodeType.OutRoute)
            return double.PositiveInfinity;

        return link.Tag;
    }

    public static bool TryGetShortestPath<TLink>(this IVertexAndEdgeListGraph<INetNode, TLink> graph, INetNode source, INetNode target, [NotNullWhen(true)] out IEnumerable<TLink>? path)
        where TLink : ILink
    {
        var tryFunc = graph.ShortestPathsDijkstra(CalculateLength, source);
        if (tryFunc(target, out path))
            return true;
        path = null;
        return false;
    }

    public static async Task<List<List<TLink>>> FindOpticPathsAsync<TLink>(
        this IVertexAndEdgeListGraph<INetNode, TLink> graph,
        INetNode source, INetNode target,
        int k = 10)
        where TLink : ILink
    {

        if (!graph.TryGetShortestPath(source, target, out var path))
            throw new InvalidOperationException("No initial shortest path found");

        var result = new List<List<TLink>>() { path!.ToList() };
        var candidates = new List<(double Cost, List<TLink> Path)>();

        for (int i = 2; i <= k; i++)
        {
            var lastPath = result[^1];
            var lastNodes = GetNodesFromPath(lastPath);

            var t = new List<Task<IEnumerable<TLink>?>>();
            for (int spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                int capturedSpurIdx = spurIdx;
                var task = Task.Run(() =>
                {
                    var spurNode = lastNodes[capturedSpurIdx];
                    var rootNodes = lastNodes.GetRange(0, capturedSpurIdx + 1);
                    var rootEdges = lastPath.Take(capturedSpurIdx).ToList();

                    var removedEdges = new HashSet<TLink>();
                    foreach (var p in result)
                    {
                        var pNodes = GetNodesFromPath(p);
                        if (pNodes.Count > capturedSpurIdx + 1 && pNodes.GetRange(0, capturedSpurIdx + 1).SequenceEqual(rootNodes))
                            removedEdges.Add(p[capturedSpurIdx]);
                    }

                    Func<TLink, double> weight = e => removedEdges.Contains(e) ? float.PositiveInfinity : e.CalculateLength();

                    var tryGet = graph.ShortestPathsDijkstra(weight, spurNode);
                    if (!tryGet(target, out IEnumerable<TLink>? spurPath))
                        return null;

                    return spurPath;
                });

                t.Add(task);
            }

            var loop = (await Task.WhenAll(t)).Where(r => r != null).Select(r => r!.ToList());
            candidates.AddRange(loop.Select(p =>
            {
                return (p.Sum(l => l.Tag), p);
            }));

            // Sort candidates
            candidates.Sort((a, b) => a.Cost.CompareTo(b.Cost));

            // Move shortest candidate to results
            result.Add(candidates[0].Path);
            candidates.RemoveAt(0);
        }

        return result;
    }

    private static List<INetNode> GetNodesFromPath<TLink>(IEnumerable<TLink> path)
        where TLink : ILink
    {
        INetNode current = null!;
        var nodes = new List<INetNode>();
        var i = 0;
        foreach (var link in path)
        {
            if (i == 0)
            {
                current = link.Source;
                nodes.Add(link.Source);
                nodes.Add(link.Target);
            }
            else if (link.Source == current)
            {
                current = link.Target;
                nodes.Add(link.Target);
            }
            else
            {
                throw new InvalidOperationException("Inconsistent path");
            }
            i++;
        }
        return nodes;
    }
}