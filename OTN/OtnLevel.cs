using System;

namespace OTN;

public enum OtnLevel
{
    ODU0 = 0,
    ODU1 = 1,
    ODU2 = 2,
    ODU3 = 3,
    ODU4 = 4
}

public static class OtnLevelProperties
{
    public static int SlotsRequired(this OtnLevel type) => type switch
    {
        OtnLevel.ODU0 => 1,
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 31,
        OtnLevel.ODU4 => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static int SlotsAvailable(this OtnLevel type) => type switch
    {
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 31,
        OtnLevel.ODU4 => 80,
        _ => 0
    };

    public static double ExpectedBandwidthGbps(this OtnLevel type) => type switch
    {
        OtnLevel.ODU0 => 1.24416,
        OtnLevel.ODU1 => 2.498775,
        OtnLevel.ODU2 => 10.037274,
        OtnLevel.ODU3 => 40.319219,
        OtnLevel.ODU4 => 104.794446,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static OtnLevel OffsetLevel(this OtnLevel level, int offset)
    {
        var newValue = (int)level + offset;

        if (newValue < 0)
            newValue = 0;
        else if (newValue > (int)OtnLevel.ODU4)
            newValue = (int)OtnLevel.ODU4;

        return (OtnLevel)newValue;
    }

    public static OtnLevel NextLevel(this OtnLevel level)
    {
        return level.OffsetLevel(1);
    }
}