using OTN.Core;
using OTN.Enums;
using OTN.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace OTN.Extensions;

/// <summary>
/// Contains extension methods to convert a generic clientel signal to an OTN signal.
/// </summary>
public static class SignalExtensions
{
    private const double _tolerance = 0.001;
    /// <summary>
    /// Attempts to convert a generic signal to an OTN signal at a specified OTN level.
    /// </summary>
    /// <param name="signal">The generic signal.</param>
    /// <param name="oduLevel">The targeted OTN level.</param>
    /// <param name="result">When this method returns, contains the OTN signal if conversion is successful; otherwise, null.</param>
    /// <returns><c>true</c> if the conversion is successful; otherwise, <c>false</c>.</returns>
    public static bool TryToOtnSignal(this ISignal signal, OtnLevel oduLevel, [NotNullWhen(true)] out OtnSignal? result)
    {
        result = null;
        var expected = oduLevel.ExpectedBandwidthGbps();

        // Relaxed bandwidth check: 
        // signal bandwidth must not exceed container bandwidth + tolerance
        if (signal.BandwidthGbps > expected + _tolerance)
            return false;

        result = new OtnSignal(Enum.GetName(oduLevel)!,
                               expected,
                               oduLevel,
                               signal.Source,
                               signal.Target);

        return true;
    }

    /// <summary>
    /// Converts a generic signal to the minimal suitable OTN signal level.
    /// </summary>
    /// <param name="signal">The generic signal.</param>
    /// <returns>The converted OTN signal.</returns>
    /// <exception cref="InvalidOperationException">Thrown when conversion to any OTN level fails.</exception>
    public static OtnSignal ToOtnSignal(this ISignal signal)
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
