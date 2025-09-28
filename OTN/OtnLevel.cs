using System;

namespace OTN;

/// <summary>
/// Enumerates the Optical Transport Network (OTN) levels 
/// representing various ODU (Optical Data Unit) signal levels.
/// </summary>
public enum OtnLevel
{
    ODU0 = 0,
    ODU1 = 1,
    ODU2 = 2,
    ODU3 = 3,
    ODU4 = 4
}

/// <summary>
/// Provides extension methods for working with <see cref="OtnLevel"/> enumeration.
/// </summary>
public static class OtnLevelProperties
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
        OtnLevel.ODU3 => 31,
        OtnLevel.ODU4 => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// Gets the number of slots available for the given OTN level.
    /// </summary>
    /// <param name="type">The OTN level.</param>
    /// <returns>The number of slots available, or 0 if none are available.</r
    public static int SlotsAvailable(this OtnLevel type) => type switch
    {
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 31,
        OtnLevel.ODU4 => 80,
        _ => 0
    };

    /// <summary>
    /// Gets the expected bandwidth in Gbps for the given OTN level.
    /// </summary>
    /// <param name="type">The OTN level.</param>
    /// <returns>The expected bandwidth in gigabits per second.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the OTN lev
    public static double ExpectedBandwidthGbps(this OtnLevel type) => type switch
    {
        OtnLevel.ODU0 => 1.24416,
        OtnLevel.ODU1 => 2.498775,
        OtnLevel.ODU2 => 10.037274,
        OtnLevel.ODU3 => 40.319219,
        OtnLevel.ODU4 => 104.794446,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    /// <summary>
    /// Returns a new OTN level offset by a specified amount.
    /// </summary>
    /// <param name="level">The current OTN level.</param>
    /// <param name="offset">The offset to apply (positive or negative).</param>
    /// <returns>The offset OTN level, bounded to the defined range.</returns>
    public static OtnLevel OffsetLevel(this OtnLevel level, int offset)
    {
        var newValue = (int)level + offset;

        if (newValue < 0)
            newValue = 0;
        else if (newValue > (int)OtnLevel.ODU4)
            newValue = (int)OtnLevel.ODU4;

        return (OtnLevel)newValue;
    }

    /// <summary>
    /// Returns the next higher OTN level if available.
    /// </summary>
    /// <param name="level">The current OTN level.</param>
    /// <returns>The next OTN level, or the current level if already at maximu
    public static OtnLevel NextLevel(this OtnLevel level)
    {
        return level.OffsetLevel(1);
    }
}