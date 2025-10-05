using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;
using QuikGraph;
using System;
using System.Collections.Generic;

namespace OTN.Core;

/// <summary>
/// Network as bidirectional graph
/// </summary>
public class Network : INetwork
{
    private readonly Dictionary<Guid, IOtnSignal> _signalMap
        = new Dictionary<Guid, IOtnSignal>();

    public Guid Id { get; } = Guid.NewGuid();

    public BidirectionalGraph<INetNode, ILink> Optical { get; }
        = new BidirectionalGraph<INetNode, ILink>(true);

    public BidirectionalGraph<IOtnNode, ISignal> Electrical { get; }
        = new BidirectionalGraph<IOtnNode, ISignal>();

    public INetNode AddNetNode()
    {
        var n = new Node();
        if (!Optical.AddVertex(n))
            throw new InvalidOperationException();
        return n;
    }

    public ILink AddLink(INetNode source, INetNode target, double length, LinkType linkType = LinkType.Undirected)
    {
        var l = new Link(source, target, length, linkType);
        if (!Optical.AddEdge(l))
            throw new InvalidOperationException();
        else if (linkType == LinkType.Undirected && !Optical.AddEdge(l.Reverse!))
            throw new InvalidOperationException();
        return l;
    }

    public IOtnNode AddOtnNode(INetNode node, IEnumerable<AggregationRule> rules, int capacity = 1)
    {
        return AddOtnNode(node, rules, OtnSettings.Default, capacity);
    }

    public IOtnNode AddOtnNode(INetNode node, IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1)
    {
        var n = new OtnNode(node, rules, settings, capacity);
        if (!Electrical.AddVertex(n))
            throw new InvalidOperationException();
        return n;
    }

    public IOtnSignal AddSignal(IOtnNode source, IOtnNode target, double bandwidthGbps, AggregationStrategy strategy = AggregationStrategy.NextFit)
    {
        return AddSignal(new Signal(source, target, bandwidthGbps), strategy);
    }

    public IOtnSignal AddSignal(ISignal signal, AggregationStrategy strategy = AggregationStrategy.NextFit)
    {
        if (signal is IOtnSignal)
            throw new InvalidOperationException();
        else if (!(Electrical.ContainsVertex(signal.Source) && Electrical.ContainsVertex(signal.Target)))
            throw new InvalidOperationException();

        var s = signal.ToOtnSignal();
        if (!(s.Source.TryAggregate(s, out var aggregated, strategy) && s.Target.TryAggregate(aggregated, out _, strategy)))
            throw new InvalidOperationException();

        _signalMap.Add(signal.Id, s);

        return s;
    }
}