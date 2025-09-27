using System;
using System.Diagnostics.CodeAnalysis;

namespace OTN;

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

public static class SignalToOTN
{
    private const double _tolerance = 0.001;
    public static bool TryToOtnSignal(this Signal signal, OtnLevel oduLevel, [NotNullWhen(true)] out OtnSignal? result)
    {
        result = null;

        if (Math.Abs(signal.BandwidthGbps - oduLevel.ExpectedBandwidthGbps()) > _tolerance)
            return false; // Bandwidth mismatch

        result = new OtnSignal(signal.Id, signal.Name, signal.BandwidthGbps, oduLevel);
        return true;
    }

    public static OtnSignal ToOtnSignal(this Signal signal)
    {
        var minimalLevel = OtnLevel.ODU0;
        var minDiff = double.MaxValue;

        foreach (var level in Enum.GetValues<OtnLevel>())
        {
            double diff = level.ExpectedBandwidthGbps() - signal.BandwidthGbps;
            if (diff >= -_tolerance && diff < minDiff)
            {
                minDiff = diff;
                minimalLevel = level;
            }
        }

        if (!signal.TryToOtnSignal(minimalLevel, out var result))
            throw new InvalidOperationException("Cannot convert to OTN signal");

        return result;
    }
}
