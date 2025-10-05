using OTN.Core;
using OTN.Enums;
using QuikGraph;
using QuikGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OTN.Extensions;

public static class RoutingExtensions
{
    public static double CalculateLength(this Link link)
    {
        if (link.Source.RoutingType == RouteNodeType.InRoute || link.Target.RoutingType == RouteNodeType.InRoute)
            return 0;

        if (link.Source.RoutingType == RouteNodeType.OutRoute || link.Target.RoutingType == RouteNodeType.OutRoute)
            return double.PositiveInfinity;

        return link.Tag;
    }

    public static bool TryGetShortestPath(this IVertexAndEdgeListGraph<NetNode, Link> graph, NetNode source, NetNode target, [NotNullWhen(true)] out IEnumerable<Link>? path)
    {
        var tryFunc = graph.ShortestPathsDijkstra(CalculateLength, source);
        if (tryFunc(target, out path))
            return true;
        path = null;
        return false;
    }

    public static Task<List<List<Link>>> FindOpticPathsAsync(this Network network, NetNode source, NetNode target, int k = 10)
    {
        if (network.Optical is IVertexAndEdgeListGraph<NetNode, Link> opt)
            return opt.FindOpticPathsAsync(source, target, k);
        throw new InvalidOperationException("No initial shortest path found");
    }

    public static async Task<List<List<Link>>> FindOpticPathsAsync(
        this IVertexAndEdgeListGraph<NetNode, Link> graph,
        NetNode source, NetNode target,
        int k = 10)
    {

        if (!graph.TryGetShortestPath(source, target, out var path))
            throw new InvalidOperationException("No initial shortest path found");

        var result = new List<List<Link>>() { path!.ToList() };
        var candidates = new List<(double Cost, List<Link> Path)>();

        for (int i = 2; i <= k; i++)
        {
            var lastPath = result[^1];
            var lastNodes = GetNodesFromPath(lastPath);

            var t = new List<Task<IEnumerable<Link>?>>();
            var semaphore = new SemaphoreSlim(k);
            for (int spurIdx = 0; spurIdx < lastNodes.Count - 1; spurIdx++)
            {
                int capturedSpurIdx = spurIdx;
                await semaphore.WaitAsync();
                var task = Task.Run(() =>
                {
                    try
                    {
                        var spurNode = lastNodes[capturedSpurIdx];
                        var rootNodes = lastNodes.GetRange(0, capturedSpurIdx + 1);
                        var rootEdges = lastPath.Take(capturedSpurIdx).ToList();

                        var removedEdges = new HashSet<Guid>();
                        foreach (var p in result)
                        {
                            var pNodes = GetNodesFromPath(p);
                            if (pNodes.Count > capturedSpurIdx + 1 && pNodes.GetRange(0, capturedSpurIdx + 1).SequenceEqual(rootNodes))
                                removedEdges.Add(p[capturedSpurIdx].Id);
                        }

                        Func<Link, double> weight = e => removedEdges.Contains(e.Id) ? float.PositiveInfinity : e.CalculateLength();

                        var tryGet = graph.ShortestPathsDijkstra(weight, spurNode);
                        if (!tryGet(target, out IEnumerable<Link>? spurPath))
                            return null;

                        return spurPath;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
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

    private static List<NetNode> GetNodesFromPath(IEnumerable<Link> path)
    {
        var nodes = new List<NetNode>();
        var i = 0;
        foreach (var link in path)
        {
            var current = link.Source;
            if (i == 0)
            {
                nodes.Add(current);
                nodes.Add(link.Target);
                current = link.Target;
            }
            else
            {
                if (link.Source != current)
                    throw new InvalidOperationException("Inconsistent path");

                nodes.Add(link.Target);
                current = link.Target;
            }
            i++;
        }
        return nodes;
    }
}