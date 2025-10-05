using OTN.Interfaces;
using QuikGraph;
using System;

namespace OTN.Core;

/// <summary>
/// Network as bidirectional graph
/// </summary>
public class Network : INetwork
{
    public Guid Id { get; } = Guid.NewGuid();

    public BidirectionalGraph<INetNode, ILink> Optical { get; }
        = new BidirectionalGraph<INetNode, ILink>(true);

    public BidirectionalGraph<IOtnNode, ISignal> Electrical { get; }
        = new BidirectionalGraph<IOtnNode, ISignal>();
}