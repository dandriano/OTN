using OTN.Interfaces;
using System;

namespace OTN.Core;

/// <inheritdoc />
public class Signal : ISignal
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public double BandwidthGbps { get; }

    public Signal(string name, double bandwidthGbps)
    {
        Name = name;
        BandwidthGbps = bandwidthGbps;
    }

    public override string ToString()
    {
        return $"Signal: {Name}, Id: {Id}, Bandwidth: {BandwidthGbps} Gbps";
    }
}
