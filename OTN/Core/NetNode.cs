using System;
using OTN.Enums;
using OTN.Interfaces;

namespace OTN.Core;

/// <summary>
/// Representation of a network node on a graph
/// </summary>
public class NetNode : INetNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public NetNodeType Type { get; set; }
    public NetNode(NetNodeType type)
    {
        Type = type;
    }

    public override string ToString()
    {
        return Id.ToString()[..5];
    }
}
