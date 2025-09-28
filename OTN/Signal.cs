using System;

namespace OTN;

/// <summary>
/// Represents a generic (mostly client) signal with associated bandwidth.
/// </summary>
public class Signal
{
    public Guid Id { get; }
    public string Name { get; }
    public double BandwidthGbps { get; }

    public Signal(Guid id, string name, double bandwidthGbps)
    {
        Id = id;
        Name = name;
        BandwidthGbps = bandwidthGbps;
    }

    public override string ToString()
    {
        return $"Signal: {Name}, Id: {Id}, Bandwidth: {BandwidthGbps} Gbps";
    }
}
