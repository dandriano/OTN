using OTN.Enums;
using OTN.Interfaces;
using System;

namespace OTN.Core;

/// <summary>
/// Representation of a network node on a graph
/// </summary>
public class Node : INetNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public NetNodeType Type { get; private set; }

    public Node(NetNodeType type = NetNodeType.Terminal)
    {
        Type = type;
    }
}