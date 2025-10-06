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
    LinkType LinkType { get; }
    public Link? Reverse { get; private set; }

    public Link(NetNode source, NetNode target, double weight, LinkType linkType = LinkType.Undirected) : base(source, target, weight)
    {
        LinkType = linkType;
    }

    public void SetReverse(Link reverse)
    {
        if (LinkType == LinkType.Undirected && reverse.LinkType == LinkType.Undirected)
            Reverse = reverse;
    }
}
