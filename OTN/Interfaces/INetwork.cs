using QuikGraph;
using System;

namespace OTN.Interfaces;

public interface INetwork
{
    Guid Id { get; }
    BidirectionalGraph<INetNode, ILink> Optical { get; }
    BidirectionalGraph<IOtnNode, ISignal> Electrical { get; }
}