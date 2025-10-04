using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN.Core;

/// <inheritdoc />
public class OtnSignal : Signal, IOtnSignal
{
    private readonly List<IOtnSignal> _aggregation = new List<IOtnSignal>();
    /// <inheritdoc />
    public IReadOnlyList<IOtnSignal> Aggregation => _aggregation.AsReadOnly();
    public OtnLevel OduLevel { get; }

    public IOtnNode Source => throw new NotImplementedException();

    public IOtnNode Target => throw new NotImplementedException();

    public OtnSignal(string name, double bandwidthGbps, OtnLevel oduLevel)
        : base(name, bandwidthGbps)
    {
        OduLevel = oduLevel;
    }
    
    /// <inheritdoc />
    public bool CanAggregate(IOtnSignal client, IOtnSettings settings)
    {
        const double tolerance = 0.001;

        if (client.OduLevel >= OduLevel)
            return false;

        double clientExpected = client.OduLevel.ExpectedBandwidthGbps();
        if (Math.Abs(client.BandwidthGbps - clientExpected) > tolerance)
            return false;

        double containerExpected = OduLevel.ExpectedBandwidthGbps();
        if (Math.Abs(BandwidthGbps - containerExpected) > tolerance)
            return false;

        var currentUsedSlots = _aggregation.Sum(c => settings.SlotsRequired(c.OduLevel));
        var clientSlots = settings.SlotsRequired(client.OduLevel);
        if (currentUsedSlots + clientSlots > settings.SlotsAvailable(OduLevel))
            return false;

        return true;
    }

    /// <inheritdoc />
    public bool CanAggregate(ISignal client, IOtnSettings settings, out IOtnSignal otnClient)
    {
        if (client is IOtnSignal signal)
            otnClient = signal;
        else
            otnClient = client.ToOtnSignal();

        return CanAggregate(otnClient, settings);
    }

    /// <inheritdoc />
    public bool TryAggregate(ISignal client, IOtnSettings settings)
    {
        if (!CanAggregate(client, settings, out var otnClient))
            return false;

        _aggregation.Add(otnClient);
        return true;
    }

    public override string ToString()
    {
        return base.ToString() + $", ODU Level: {OduLevel}, Aggregated Clients: {_aggregation.Count}";
    }
}