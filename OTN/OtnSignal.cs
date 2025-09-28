using OTN.Enums;
using OTN.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN;

/// <summary>
/// Represents an Optical Transport Network (OTN) signal with a specific OTU/ODU level, 
/// supporting client signal aggregation.
/// </summary>
public class OtnSignal : Signal
{
    public OtnLevel OduLevel { get; }

    private readonly List<OtnSignal> _aggregation = new List<OtnSignal>();

    public OtnSignal(Guid id, string name, double bandwidthGbps, OtnLevel oduLevel)
        : base(id, name, bandwidthGbps)
    {
        OduLevel = oduLevel;
    }

    /// <summary>
    /// Determines whether a client OTN signal can be aggregated within this container OTN signal.
    /// </summary>
    /// <param name="client">The client OTN signal to check for aggregation.</param>
    /// <returns><c>true</c> if the client can be aggregated; otherwise, <c>false</c>.</returns>
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

        var currentUsedSlots = _aggregation.Sum(c => c.OduLevel.SlotsRequired());
        var clientSlots = client.OduLevel.SlotsRequired();
        if (currentUsedSlots + clientSlots > OduLevel.SlotsAvailable())
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to aggregate a client signal within this OTN signal container.
    /// </summary>
    /// <param name="client">The client OTN signal to aggregate.</param>
    /// <returns><c>true</c> if aggregation is successful; otherwise, <c>false</c>.</returns>
    public bool TryAggregate(OtnSignal client)
    {
        if (!CanAggregate(client))
            return false;

        _aggregation.Add(client);
        return true;
    }

    public override string ToString()
    {
        return base.ToString() + $", ODU Level: {OduLevel}, Aggregated Clients: {_aggregation.Count}";
    }
}