using OTN.Interfaces;
using QuikGraph;
using System;

namespace OTN.Core;

/// <inheritdoc />
public class Signal : ISignal, IEdge<OtnNode>
{
    public Guid Id { get; } = Guid.NewGuid();
    public double BandwidthGbps { get; }
    public OtnNode Source { get; } = null!;
    public OtnNode Target { get; } = null!;

    public Signal(OtnNode source, OtnNode target, double bandwidthGbps)
    {
        BandwidthGbps = bandwidthGbps;
        Source = source;
        Target = target;
    }

    public override string ToString()
    {
        return $"Signal: {Id}, Bandwidth: {BandwidthGbps} Gbps";
    }
}
