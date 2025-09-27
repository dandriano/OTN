using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN;

public class OtnSignal : Signal
{
    public OtnLevel OduLevel { get; }

    private readonly List<OtnSignal> aggregation = [];

    public OtnSignal(Guid id, string name, double bandwidthGbps, OtnLevel oduLevel)
        : base(id, name, bandwidthGbps)
    {
        OduLevel = oduLevel;
    }

    public bool CanAggregate(OtnSignal client)
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

        var currentUsedSlots = aggregation.Sum(c => c.OduLevel.SlotsRequired());
        var clientSlots = client.OduLevel.SlotsRequired();
        if (currentUsedSlots + clientSlots > OduLevel.SlotsAvailable())
            return false;

        return true;
    }

    public bool TryAggregateClient(OtnSignal client)
    {
        if (!CanAggregate(client))
            return false;

        aggregation.Add(client);
        return true;
    }

    public override string ToString()
    {
        return base.ToString() + $", ODU Level: {OduLevel}, Aggregated Clients: {aggregation.Count}";
    }
}