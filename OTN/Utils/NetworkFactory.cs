using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using QuikGraph;

namespace OTN.Utils;

public static class NetworkFactory
{
    public static Network Create(int totalNodes, int backboneNodes, int avgEdgesPerNode, int mustPassNodes = 0, int mustAvoidNodes = 0)
    {
        var network = new Network(CreateMagistralNetworkGraph(totalNodes, backboneNodes, avgEdgesPerNode, mustPassNodes, mustAvoidNodes));

        return network;
    }

    public static Network Create(IEnumerable<Requirement<NetNode, double>> requirements)
    {
        var network = new Network(CreateRequirementsGraph(requirements));

        return network;
    }

    public static BidirectionalGraph<NetNode, Link> CreateRequirementsGraph(IEnumerable<Requirement<NetNode, double>> requirements)
    {
        var graph = new BidirectionalGraph<NetNode, Link>();
        var rand = new Random();

        var uniqueNodes = new HashSet<NetNode>();
        foreach (var r in requirements)
        {
            uniqueNodes.Add(r.Source);
            uniqueNodes.Add(r.Target);
        }

        foreach (var node in uniqueNodes)
            graph.AddVertex(node);

        // For each requirement with positive fiberCount, add links with odd/even reversal logic 
        // (paired bidirectional where possible, extra is ignored...)
        foreach (var r in requirements
            .Where(req => req.TagCount > 0))
        {
            var fiberCount = r.TagCount;
            var pairs = fiberCount / 2;
            var extra = fiberCount % 2;

            // Add bidirectional pairs (each pair represents an odd/even reversed edge)
            for (int p = 0; p < pairs; p++)
            {
                var link = new Link(r.Source, r.Target, r.Tag);
                graph.AddEdge(link);

                var reverseLink = new Link(r.Target, r.Source, r.Tag);
                graph.AddEdge(reverseLink);

                link.SetReverse(reverseLink);
                reverseLink.SetReverse(link);
            }

            // If extra
            if (extra > 0)
            {
                // do something
            }
        }

        return graph;
    }

    public static BidirectionalGraph<NetNode, Link> CreateMagistralNetworkGraph(int totalNodes, int backboneNodes, int avgEdgesPerNode, int mustPassNodes = 0, int mustAvoidNodes = 0)
    {
        if (totalNodes < 0 || backboneNodes < 0 || mustPassNodes < 0 || mustAvoidNodes < 0 || avgEdgesPerNode < 0)
            throw new ArgumentException("Input parameters cannot be negative.");
        if (mustPassNodes + mustAvoidNodes > totalNodes)
            throw new ArgumentException("Too many must-pass or must-avoid nodes.");

        var graph = new BidirectionalGraph<NetNode, Link>();
        var nodes = new List<NetNode>();

        if (backboneNodes > totalNodes)
            backboneNodes = Math.Max(1, totalNodes / 10); // limit backbone nodes

        // Create nodes
        for (var i = 0; i < totalNodes; i++)
        {
            if (mustPassNodes > 0)
            {
                mustPassNodes--;
                nodes.Add(new NetNode(NetNodeType.Terminal, RouteNodeType.InRoute));
            }
            else if (mustAvoidNodes > 0)
            {
                mustAvoidNodes--;
                nodes.Add(new NetNode(NetNodeType.OLA, RouteNodeType.OutRoute));
            }
            else
            {
                nodes.Add(new NetNode(NetNodeType.ROADM));
            }
        }

        var rand = new Random();
        // Connect backbone nodes densely (full mesh or near-full)
        for (var i = 0; i < backboneNodes; i++)
        {
            for (var j = i + 1; j < backboneNodes; j++)
            {
                var w = Math.Max(60, rand.NextDouble() * 200);
                var link = new Link(nodes[i], nodes[j], w);
                graph.AddVerticesAndEdge(link);

                var reverseLink = new Link(nodes[j], nodes[i], w);
                graph.AddEdge(reverseLink);

                link.SetReverse(reverseLink);
                reverseLink.SetReverse(link);
            }
        }

        // Connect other nodes sparsely to random backbone or close neighbors
        for (var i = backboneNodes; i < totalNodes; i++)
        {
            var connections = Math.Max(1, rand.Next(avgEdgesPerNode)); // at least 1 connection

            for (var c = 0; c < connections; c++)
            {
                // Connect either to a backbone node (hotspot) or a neighbor node in range
                NetNode connectTo;
                if (rand.NextDouble() < 0.7 && backboneNodes > 0)
                {
                    connectTo = nodes[rand.Next(backboneNodes)];  // connect to backbone node 70% chance
                }
                else
                {
                    // Connect to a node close by index for locality
                    var neighborIdx = Math.Max(backboneNodes, Math.Min(totalNodes - 1, i + rand.Next(-5, 6)));
                    connectTo = nodes[neighborIdx];
                }

                if (connectTo != nodes[i])
                {
                    var w = Math.Max(60, rand.NextDouble() * 200);
                    var link = new Link(nodes[i], connectTo, w);
                    graph.AddVerticesAndEdge(link);

                    // commented out to enforce symmetry
                    // if (rand.Next(2) == 0)
                    //    continue;

                    var reverseLink = new Link(connectTo, nodes[i], w);
                    graph.AddEdge(reverseLink);

                    link.SetReverse(reverseLink);
                    reverseLink.SetReverse(link);
                }
            }
        }

        return graph;
    }
    
    public static List<Requirement<NetNode, double>> CreateMagistralNetworkRequirements(int totalNodes, int backboneNodes, int mustPassNodes, int mustAvoidNodes = 0, int avgEdgesPerNode = 0)
    {
        if (totalNodes < 0 || backboneNodes < 0 || mustPassNodes < 0 || mustAvoidNodes < 0 || avgEdgesPerNode < 0)
            throw new ArgumentException("Input parameters cannot be negative.");
        if (mustPassNodes + mustAvoidNodes > totalNodes)
            throw new ArgumentException("Too many must-pass or must-avoid nodes.");

        var nodes = new List<NetNode>();
        var requirements = new List<Requirement<NetNode, double>>();
        var rand = new Random();

        backboneNodes = Math.Min(backboneNodes, totalNodes);
        if (backboneNodes == 0 && totalNodes > 0)
            backboneNodes = Math.Max(1, totalNodes / 10);       // limit backbone

        // Create nodes
        for (var i = 0; i < totalNodes; i++)
        {
            if (mustPassNodes > 0)
            {
                mustPassNodes--;
                nodes.Add(new NetNode(NetNodeType.Terminal, RouteNodeType.InRoute));
            }
            else if (mustAvoidNodes > 0)
            {
                mustAvoidNodes--;
                nodes.Add(new NetNode(NetNodeType.OLA, RouteNodeType.OutRoute));
            }
            else
            {
                nodes.Add(new NetNode(NetNodeType.ROADM, RouteNodeType.Undefined));
            }
        }

        // Create requirements for backbone nodes (dense mesh)
        for (var i = 0; i < backboneNodes; i++)
        {
            for (var j = i + 1; j < backboneNodes; j++)
            {
                var w = Math.Max(60, rand.NextDouble() * 200);
                // Bidirectional requirement (FiberCount = 2 for forward and reverse)
                requirements.Add(new Requirement<NetNode, double>(nodes[i], nodes[j], w));
            }
        }

        // Create requirements for non-backbone nodes (sparse connections)
        for (var i = backboneNodes; i < totalNodes; i++)
        {
            var connections = Math.Max(1, rand.Next(avgEdgesPerNode));
            for (var c = 0; c < connections; c++)
            {
                NetNode connectTo;
                if (rand.NextDouble() < 0.7 && backboneNodes > 0)
                {
                    connectTo = nodes[rand.Next(backboneNodes)]; // Connect to backbone node
                }
                else
                {
                    var neighborIdx = Math.Max(backboneNodes, Math.Min(totalNodes - 1, i + rand.Next(-5, 6)));
                    connectTo = nodes[neighborIdx]; // Connect to nearby node
                }

                if (connectTo != nodes[i])
                {
                    var w = Math.Max(60, rand.NextDouble() * 200);
                    // Randomly decide if bidirectional (FiberCount = 2) or unidirectional (FiberCount = 1)
                    var fc = rand.Next(2) == 0 ? 2 : 1;
                    requirements.Add(new Requirement<NetNode, double>(nodes[i], connectTo, w, fc));
                }
            }
        }

        return requirements;
    }
}