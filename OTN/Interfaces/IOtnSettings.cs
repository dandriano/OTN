using System;
using OTN.Enums;

namespace OTN.Interfaces;

public interface IOtnSettings
{
    int SlotsRequired(OtnLevel level) => level switch
    {
        OtnLevel.ODU0 => 1,
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 32,
        OtnLevel.ODU4 => 80,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    int SlotsAvailable(OtnLevel level) => level switch
    {
        OtnLevel.ODU1 => 2,
        OtnLevel.ODU2 => 8,
        OtnLevel.ODU3 => 32,
        OtnLevel.ODU4 => 80,
        _ => 0
    };
}
