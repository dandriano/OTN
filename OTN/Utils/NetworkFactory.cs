using System;
using System.Collections.Generic;
using OTN.Core;
using OTN.Enums;
using QuikGraph;

namespace OTN.Utils;

public static class NetworkFactory
{
    public static Network Create(int totalNodes, int backboneNodes, int avgEdgesPerNode)
    {
        var network = new Network(CreateMagistralNetworkGraph(totalNodes, backboneNodes, avgEdgesPerNode));

        return network;
    }
    public static BidirectionalGraph<NetNode, Link> CreateMagistralNetworkGraph(int totalNodes, int backboneNodes, int avgEdgesPerNode)
    {
        var graph = new BidirectionalGraph<NetNode, Link>();
        var nodes = new List<NetNode>();

        if (backboneNodes > totalNodes)
            backboneNodes = totalNodes / 10; // limit backbone nodes

        // Create nodes
        for (int i = 0; i < totalNodes; i++)
            nodes.Add(new NetNode(NetNodeType.Terminal));

        var rand = new Random();

        // Connect backbone nodes densely (full mesh or near-full)
        for (int i = 0; i < backboneNodes; i++)
        {
            for (int j = i + 1; j < backboneNodes; j++)
            {
                var w = rand.NextDouble() * 10 + 1;
                var link = new Link(nodes[i], nodes[j], w);
                graph.AddVerticesAndEdge(link);

                var reverseLink = new Link(nodes[j], nodes[j], w);
                graph.AddEdge(reverseLink);

                link.SetReverse(reverseLink);
                reverseLink.SetReverse(link);
            }
        }

        // Connect other nodes sparsely to random backbone or close neighbors
        for (int i = backboneNodes; i < totalNodes; i++)
        {
            int connections = Math.Max(1, rand.Next(avgEdgesPerNode)); // at least 1 connection

            for (int c = 0; c < connections; c++)
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
                    int neighborIdx = Math.Max(backboneNodes, Math.Min(totalNodes - 1, i + rand.Next(-5, 6)));
                    connectTo = nodes[neighborIdx];
                }

                if (connectTo != nodes[i])
                {
                    var w = rand.NextDouble() * 10 + 1;
                    var link = new Link(nodes[i], connectTo, w);
                    graph.AddVerticesAndEdge(link);

                    if (rand.Next(2) == 0)
                        continue;

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