using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;
using QuikGraph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN.Core;

/// <inheritdoc />
public class OtnSignal : Signal, IOtnSignal
{
    private readonly Dictionary<Guid, OtnSignal> _aggregation
        = new Dictionary<Guid, OtnSignal>();

    /// <inheritdoc />
    public IEnumerable<IOtnSignal> Signals => _aggregation.Values;
    public int SignalCount => _aggregation.Count;
    public OtnLevel OduLevel { get; }

    IOtnNode IEdge<IOtnNode>.Source => Source;

    IOtnNode IEdge<IOtnNode>.Target => Target;

    public OtnSignal(OtnNode source, OtnNode target, double bandwidthGbps, OtnLevel oduLevel)
    : base(source, target, bandwidthGbps)
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

        var currentUsedSlots = _aggregation.Sum(c => settings.SlotsRequired(c.Value.OduLevel));
        var clientSlots = settings.SlotsRequired(client.OduLevel);
        if (currentUsedSlots + clientSlots > settings.SlotsAvailable(OduLevel))
            return false;

        return true;
    }

    /// <inheritdoc />
    public bool CanAggregate(Signal client, IOtnSettings settings, out IOtnSignal otnClient)
    {
        if (client is IOtnSignal signal)
            otnClient = signal;
        else
            otnClient = client.ToOtnSignal();

        return CanAggregate(otnClient, settings);
    }

    /// <inheritdoc />
    public bool TryAggregate(OtnSignal otnClient, IOtnSettings settings)
    {
        if (!CanAggregate(otnClient, settings))
            return false;

        _aggregation.Add(otnClient.Id, otnClient);
        return true;
    }

    /// <inheritdoc />
    public bool TryDeAggregate(OtnSignal otnClient)
    {
        // Direct removal
        if (_aggregation.Remove(otnClient.Id))
            return true;

        // Recursive removal in subtrees
        foreach (var sub in _aggregation.ToList())
        {
            if (sub.Value.TryDeAggregate(otnClient))
            {
                if (sub.Value.SignalCount == 0)
                    _aggregation.Remove(sub.Key);
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        return base.ToString() + $", ODU Level: {OduLevel}, Aggregation count: {_aggregation.Count}";
    }
}