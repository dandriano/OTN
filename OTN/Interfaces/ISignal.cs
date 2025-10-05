using System;
using QuikGraph;

namespace OTN.Interfaces;

/// <summary>
/// Represents a generic (mostly client) signal with associated bandwidth.
/// </summary>
public interface ISignal
{
    Guid Id { get; }
    double BandwidthGbps { get; }
}