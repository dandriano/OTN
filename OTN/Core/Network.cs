using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly BidirectionalGraph<NetNode, Link> _optical 
        = new BidirectionalGraph<NetNode, Link>();
    private readonly BidirectionalGraph<OtnNode, Signal> _electrical 
        = new BidirectionalGraph<OtnNode, Signal>();
    private readonly Dictionary<Guid, OtnSignal> _signalMap
        = new Dictionary<Guid, OtnSignal>();

    public Guid Id { get; } = Guid.NewGuid();

    public IVertexAndEdgeListGraph<NetNode, Link> Optical => _optical;

    public IVertexAndEdgeListGraph<OtnNode, Signal> Electrical => _electrical;
    
    public Network() {}

    public Network(IVertexAndEdgeListGraph<NetNode, Link> optical)
    {
        ArgumentNullException.ThrowIfNull(optical);
        _optical.AddVertexRange(optical.Vertices);
        _optical.AddEdgeRange(optical.Edges);
    }

    public INetNode Add(NetNodeType type)
    {
        var n = new NetNode(type);
        if (!_optical.AddVertex(n))
            throw new InvalidOperationException();
        return n;
    }

    public Link Add(NetNode source, NetNode target, double length, LinkType type)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(source, target))
            throw new ArgumentException("A node cannot be linked to itself.", nameof(target));
        if (!double.IsFinite(length) || length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Link weight must be a finite non-negative value.");
        if (!_optical.ContainsVertex(source) || !_optical.ContainsVertex(target))
            throw new ArgumentException("Both link endpoints must belong to this network.");
        if (type is not LinkType.Simplex and not LinkType.Duplex)
            throw new ArgumentOutOfRangeException(nameof(type));

        var l = new Link(source, target, length, type);
        if (!_optical.AddEdge(l))
            throw new InvalidOperationException();

        if (type == LinkType.Duplex)
        {
            var reverse = new Link(l);
            if (!_optical.AddEdge(reverse))
            {
                _optical.RemoveEdge(l);
                throw new InvalidOperationException();
            }
        }

        return l;
    }

    /// <summary>Replaces a link and, when present, its reverse edge with links of the requested type and weight.</summary>
    public Link Update(Link link, double length, LinkType type)
    {
        ArgumentNullException.ThrowIfNull(link);
        if (!double.IsFinite(length) || length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Link weight must be a finite non-negative value.");
        if (type is not LinkType.Simplex and not LinkType.Duplex)
            throw new ArgumentOutOfRangeException(nameof(type));
        if (!Contains(link))
            throw new ArgumentException("The link is not part of this network.", nameof(link));

        var source = link.Source;
        var target = link.Target;

        Remove(link);
        return Add(source, target, length, type);
    }

    /// <summary>Removes a node and all optical links incident to it.</summary>
    /// <returns><see langword="true"/> if the node was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(NetNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _optical.RemoveVertex(node);
    }

    /// <summary>Removes a link and its reverse edge, if present.</summary>
    /// <returns><see langword="true"/> if the link was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        if (!Contains(link))
            return false;

        if (!_optical.RemoveEdge(link))
            return false;

        if (link.Reverse is { } reverse)
            _optical.RemoveEdge(reverse);

        return true;
    }

    private bool Contains(Link link)
        => _optical.TryGetEdges(link.Source, link.Target, out var edges) &&
            edges.Any(existing => ReferenceEquals(existing, link));

    public IOtnNode AddOtnNode(NetNode node, IEnumerable<AggregationRule> rules, int capacity = 1)
    {
        return AddOtnNode(node, rules, OtnSettings.Default, capacity);
    }

    public IOtnNode AddOtnNode(NetNode node, IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1)
    {
        var n = new OtnNode(node, rules, settings, capacity);
        if (!_electrical.AddVertex(n))
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
        else if (!(_electrical.ContainsVertex(signal.Source) && _electrical.ContainsVertex(signal.Target)))
            throw new InvalidOperationException();

        // TODO: use _requirements to decide aggregation
        var s = signal.ToOtnSignal();
        if (!(s.Source.TryAggregate(s, out var aggregated, strategy) && s.Target.TryAggregate(aggregated, out _, strategy)))
            throw new InvalidOperationException();

        _signalMap.Add(signal.Id, s);

        return s;
    }
}
