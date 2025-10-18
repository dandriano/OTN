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
}