using OTN.Enums;
using System;

namespace OTN.Extensions;

/// <summary>
/// Provides extension methods for working with <see cref="OtnLevel"/> enumeration.
/// </summary>
public static class OtnLevelExtensions
{
    /// <summary>
    /// Gets the number of slots required by the given OTN level.
    /// </summary>
    /// <param name="type">The OTN level.</param>
    /// <returns>The number of slots required.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the OTN level is not recognized.</exception>
    public static int SlotsRequired(this OtnLevel type) => type switch
    {
        OtnLevel.ODU0 => 1,
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 32,
        OtnLevel.ODU4 => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// Gets the number of slots available for the given OTN level.
    /// </summary>
    /// <param name="type">The OTN level.</param>
    /// <returns>The number of slots available, or 0 if none are available.</returns>
    public static int SlotsAvailable(this OtnLevel type) => type switch
    {
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 32,
        OtnLevel.ODU4 => 80,
        _ => 0
    };

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