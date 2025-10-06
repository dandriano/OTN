using System;
using System.Collections.Generic;
using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;
using QuikGraph;

namespace OTN.Core;

/// <summary>
/// Network as bidirectional graph
/// </summary>
public class Network
{
    private readonly Dictionary<Guid, OtnSignal> _signalMap
        = new Dictionary<Guid, OtnSignal>();

    public Guid Id { get; } = Guid.NewGuid();

    public BidirectionalGraph<NetNode, Link> Optical { get; }
        = new BidirectionalGraph<NetNode, Link>();

    public BidirectionalGraph<OtnNode, Signal> Electrical { get; }
        = new BidirectionalGraph<OtnNode, Signal>();
    
    public Network() {}

    public Network(BidirectionalGraph<NetNode, Link> optical)
    {
        Optical = optical;
    }

    public INetNode AddNetNode(NetNodeType type)
    {
        var n = new NetNode(type);
        if (!Optical.AddVertex(n))
            throw new InvalidOperationException();
        return n;
    }

    public ILink AddLink(NetNode source, NetNode target, double length, LinkType linkType = LinkType.Undirected)
    {
        var l = new Link(source, target, length, linkType);
        Link reverse = null!;
        if (linkType == LinkType.Undirected)
        {
            reverse = new Link(target, source, length, linkType);
            l.SetReverse(reverse);
            reverse.SetReverse(l);
        }

        if (!Optical.AddEdge(l))
            throw new InvalidOperationException();
        else if (linkType == LinkType.Undirected && !Optical.AddEdge(l.Reverse!))
            throw new InvalidOperationException();
        return l;
    }

    public IOtnNode AddOtnNode(NetNode node, IEnumerable<AggregationRule> rules, int capacity = 1)
    {
        return AddOtnNode(node, rules, OtnSettings.Default, capacity);
    }

    public IOtnNode AddOtnNode(NetNode node, IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1)
    {
        var n = new OtnNode(node, rules, settings, capacity);
        if (!Electrical.AddVertex(n))
            throw new InvalidOperationException();
        return n;
    }

    public IOtnSignal AddSignal(OtnNode source, OtnNode target, double bandwidthGbps, AggregationStrategy strategy = AggregationStrategy.NextFit)
    {
        return AddSignal(new Signal(source, target, bandwidthGbps), strategy);
    }

    public IOtnSignal AddSignal(Signal signal, AggregationStrategy strategy = AggregationStrategy.NextFit)
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