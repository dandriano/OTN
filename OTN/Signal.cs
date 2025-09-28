using System;
using System.Diagnostics.CodeAnalysis;

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

/// <summary>
/// Contains extension methods to convert a generic clientel signal to an OTN signal.
/// </summary>
public static class SignalToOTN
{
    private const double _tolerance = 0.001;
    /// <summary>
    /// Attempts to convert a generic signal to an OTN signal at a specified OTN level.
    /// </summary>
    /// <param name="signal">The generic signal.</param>
    /// <param name="oduLevel">The targeted OTN level.</param>
    /// <param name="result">When this method returns, contains the OTN signal if conversion is successful; otherwise, null.</param>
    /// <returns><c>true</c> if the conversion is successful; otherwise, <c>false</c>.</returns>
    public static bool TryToOtnSignal(this Signal signal, OtnLevel oduLevel, [NotNullWhen(true)] out OtnSignal? result)
    {
        result = null;

        if (Math.Abs(signal.BandwidthGbps - oduLevel.ExpectedBandwidthGbps()) > _tolerance)
            return false; // Bandwidth mismatch

        result = new OtnSignal(signal.Id, signal.Name, signal.BandwidthGbps, oduLevel);
        return true;
    }

    /// <summary>
    /// Converts a generic signal to the minimal suitable OTN signal level.
    /// </summary>
    /// <param name="signal">The generic signal.</param>
    /// <returns>The converted OTN signal.</returns>
    /// <exception cref="InvalidOperationException">Thrown when conversion to any OTN level fails.</exception>
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
