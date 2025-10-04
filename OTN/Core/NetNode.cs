using OTN.Enums;
using OTN.Interfaces;
using QuikGraph;
using System;


namespace OTN.Core;

/// <summary>
/// Representation of a network node on a graph
/// </summary>
public class Node : BidirectionalGraph<IOtnNode, IOtnSignal>, INetNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public NetNodeType Type { get; private set; }
    public bool IsInRoute { get; private set; }
    public bool IsOutRoute { get; private set; }

    public Node(string name, NetNodeType type = NetNodeType.Terminal)
    {
        Name = name;
        Type = type;
    }
}