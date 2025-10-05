using QuikGraph;
using System;

namespace OTN.Interfaces;

/// <summary>
/// Represents a generic (mostly client) signal with associated bandwidth.
/// </summary>
public interface ISignal : IEdge<IOtnNode>
{
    Guid Id { get; }
    double BandwidthGbps { get; }
}