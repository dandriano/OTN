using System;
using OTN.Enums;

namespace OTN.Interfaces;

public interface INetNode
{
    Guid Id { get; }
    NetNodeType Type { get; }
    RouteNodeType RoutingType { get; }
}