using OTN.Interfaces;
using System;

namespace OTN.Core;

/// <inheritdoc />
public class Signal : ISignal
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public double BandwidthGbps { get; }
    public IOtnNode Source { get; } = null!;
    public IOtnNode Target { get; } = null!;

    public Signal(string name, double bandwidthGbps, IOtnNode source, IOtnNode target)
    {
        Name = name;
        BandwidthGbps = bandwidthGbps;
        Source = source;
        Target = target;
    }

    public override string ToString()
    {
        return $"Signal: {Name}, Id: {Id}, Bandwidth: {BandwidthGbps} Gbps";
    }
}
