using System;
using OTN.Enums;
using OTN.Interfaces;
using QuikGraph;

namespace OTN.Core;

/// <summary>
/// Physical link as a fiber span between network nodes/circuit-packs
/// </summary>
public class Link : EquatableTaggedEdge<NetNode, double>, IEdge<NetNode>, ILink
{
    public Guid Id { get; } = Guid.NewGuid();
    public LinkType LinkType { get; private set; }
    public Link? Reverse { get; private set; }

    public Link(NetNode source, NetNode target, double weight, LinkType linkType = LinkType.Duplex)
        : base(source, target, weight)
    {
        LinkType = linkType;
    }

    /// <summary>
    /// Constructs the reverse link for an existing duplex link and cross-wires their Reverse properties.
    /// </summary>
    public Link(Link reverse) : base(reverse.Target, reverse.Source, reverse.Tag)
    {
        if (reverse.LinkType != LinkType.Duplex)
            throw new ArgumentException("Cannot create a reverse link for a simplex link.", nameof(reverse));

        if (reverse.Reverse is not null)
            throw new InvalidOperationException("Link already has a reverse link assigned.");

        Reverse = reverse;
        reverse.Reverse = this;
        LinkType = LinkType.Duplex;
    }
}
