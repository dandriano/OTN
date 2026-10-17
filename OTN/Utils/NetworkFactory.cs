using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using QuikGraph;

namespace OTN.Utils;

public static class NetworkFactory
{
    public static Network Create(int totalNodes, int backboneNodes, int connectionAttemptUpperBound)
    {
        return Create(totalNodes, backboneNodes, connectionAttemptUpperBound, new Random());
    }

    public static Network Create(int totalNodes, int backboneNodes, int connectionAttemptUpperBound, int seed)
    {
        return Create(totalNodes, backboneNodes, connectionAttemptUpperBound, new Random(seed));
    }

    private static Network Create(int totalNodes, int backboneNodes, int connectionAttemptUpperBound, Random random)
    {
        return new Network(CreateMagistralNetworkGraph(totalNodes, backboneNodes, connectionAttemptUpperBound, random));
    }

    public static BidirectionalGraph<NetNode, Link> CreateMagistralNetworkGraph(int totalNodes, int backboneNodes, int connectionAttemptUpperBound)
    {
        return CreateMagistralNetworkGraph(totalNodes, backboneNodes, connectionAttemptUpperBound, new Random());
    }

    public static BidirectionalGraph<NetNode, Link> CreateMagistralNetworkGraph(int totalNodes, int backboneNodes, int connectionAttemptUpperBound, int seed)
    {
        return CreateMagistralNetworkGraph(totalNodes, backboneNodes, connectionAttemptUpperBound, new Random(seed));
    }

    private static BidirectionalGraph<NetNode, Link> CreateMagistralNetworkGraph(int totalNodes, int backboneNodes, int connectionAttemptUpperBound, Random rand)
    {
        if (totalNodes < 0 || backboneNodes < 0 || connectionAttemptUpperBound < 0)
            throw new ArgumentException("Input parameters cannot be negative.");

        var graph = new BidirectionalGraph<NetNode, Link>();
        var nodes = new List<NetNode>();

        if (backboneNodes > totalNodes)
            backboneNodes = Math.Max(1, totalNodes / 10); // limit backbone nodes

        // Create nodes
        for (var i = 0; i < totalNodes; i++)
            nodes.Add(new NetNode((NetNodeType)rand.Next(3)));

        // Connect backbone nodes densely (full mesh or near-full)
        for (var i = 0; i < backboneNodes; i++)
        {
            for (var j = i + 1; j < backboneNodes; j++)
            {
                var w = Math.Max(60, rand.NextDouble() * 200);
                var link = new Link(nodes[i], nodes[j], w);
                graph.AddVerticesAndEdge(link);
                graph.AddEdge(new Link(link));
            }
        }

        // Connect other nodes sparsely to random backbone or close neighbors
        for (var i = backboneNodes; i < totalNodes; i++)
        {
            var connections = Math.Max(1, rand.Next(connectionAttemptUpperBound)); // exclusive upper bound; at least one attempt

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
                    graph.AddEdge(new Link(link));
                }
            }
        }

        return graph;
    }
}
