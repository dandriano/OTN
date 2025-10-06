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
    public NetNodeType Type { get; private set; }
    public RouteNodeType RoutingType { get; private set; } = RouteNodeType.Undefined;

    public NetNode(NetNodeType type)
    {
        Type = type;
    }
}