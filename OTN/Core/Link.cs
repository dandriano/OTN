using OTN.Enums;
using OTN.Interfaces;
using QuikGraph;
using System;

namespace OTN.Core;

/// <summary>
/// Physical link as a fiber span between network nodes/circuit-packs
/// </summary>
public class Link : EquatableTaggedEdge<INetNode, double>, ILink
{
    public Guid Id { get; init; } = Guid.NewGuid();
    LinkType LinkType { get; }
    public Link? Reverse { get; private set; }

    protected Link(Node source, Node target, long weight, LinkType linkType) : base(source, target, weight)
    {
        LinkType = linkType;
        if (LinkType == LinkType.Undirected)
            Reverse = new Link(target, source, weight, linkType);
    }

    private void SetReverse(Link reverse)
    {
        Reverse = reverse;
    }
}
