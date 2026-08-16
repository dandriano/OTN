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
    public LinkType LinkType { get; }
    public Link? Reverse { get; private set; }

    public Link(NetNode source, NetNode target, double weight, LinkType linkType = LinkType.Undirected) 
        : base(source, target, weight)
    {
        LinkType = linkType;
    }

    /// <summary>
    /// Constructs the reverse link for an existing undirected link and cross-wires their Reverse properties.
    /// </summary>
    public Link(Link reverse) : base(reverse.Target, reverse.Source, reverse.Tag)
    {
        if (reverse.LinkType != LinkType.Undirected)
            throw new ArgumentException("Cannot create a reverse link for a directed link.", nameof(reverse));

        if (reverse.Reverse is not null)
            throw new InvalidOperationException("Link already has a reverse link assigned.");

        LinkType = reverse.LinkType;
        Reverse = reverse;
        reverse.Reverse = this;
    }
}
