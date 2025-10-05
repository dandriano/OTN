using OTN.Interfaces;
using System;

namespace OTN.Core;

/// <inheritdoc />
public class Signal : ISignal
{
    public Guid Id { get; } = Guid.NewGuid();
    public double BandwidthGbps { get; }
    public IOtnNode Source { get; } = null!;
    public IOtnNode Target { get; } = null!;

    public Signal(IOtnNode source, IOtnNode target, double bandwidthGbps)
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
