using OTN.Interfaces;
using QuikGraph;

namespace OTN.Core;

/// <summary>
/// Network as bidirectional graph
/// </summary>
public class Network : BidirectionalGraph<INetNode, ILink> { }