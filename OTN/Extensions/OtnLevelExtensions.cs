using System;
using OTN.Enums;

namespace OTN.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="OtnLevel"/> enumeration.
/// </summary>
public static class OtnLevelExtensions
{
    /// <summary>
    /// Gets the expected bandwidth in Gbps for the given OTN level.
    /// </summary>
    /// <param name="type">The OTN level.</param>
    /// <returns>The expected bandwidth in gigabits per second.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the OTN level is not recognized.</exception>
    public static double ExpectedBandwidthGbps(this OtnLevel type) => type switch
    {
        OtnLevel.ODU0 => 1.24416,
        OtnLevel.ODU1 => 2.498775,
        OtnLevel.ODU2 => 10.037274,
        OtnLevel.ODU3 => 40.319219,
        OtnLevel.ODU4 => 104.794446,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}