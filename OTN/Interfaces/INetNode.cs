using OTN.Enums;
using System;

namespace OTN.Interfaces;

public interface INetNode
{
    Guid Id { get; }
    NetNodeType Type { get; }
    RouteNodeType RoutingType { get; }
}