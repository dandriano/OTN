using OTN.Core;
using OTN.Enums;
using QuikGraph;
using System;
using System.Collections.Generic;

namespace OTN.Interfaces;

public interface INetwork
{
    Guid Id { get; }
    BidirectionalGraph<INetNode, ILink> Optical { get; }
    BidirectionalGraph<IOtnNode, ISignal> Electrical { get; }

    INetNode AddNetNode(NetNodeType type);
    ILink AddLink(INetNode source, INetNode target, double length, LinkType linkType = LinkType.Undirected);
    IOtnNode AddOtnNode(INetNode node, IEnumerable<AggregationRule> rules, int capacity = 1);
    IOtnNode AddOtnNode(INetNode node, IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1);
    IOtnSignal AddSignal(IOtnNode source, IOtnNode target, double bandwidthGbps, AggregationStrategy strategy = AggregationStrategy.NextFit);
}