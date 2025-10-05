using OTN.Enums;
using System.Collections.Generic;

namespace OTN.Interfaces;

/// <summary>
/// Represents an Optical Transport Network (OTN) signal with a specific OTU/ODU level, 
/// supporting client signal aggregation.
/// </summary>
public interface IOtnSignal : ISignal
{
    /// <summary>
    /// LO and intermediate OTN signal aggregation
    /// </summary>
    IEnumerable<IOtnSignal> Signals { get; }
    int SignalCount { get; }
    OtnLevel OduLevel { get; }
    /// <summary>
    /// Determines whether a client OTN signal can be aggregated within this container OTN signal.
    /// </summary>
    /// <param name="client">The client OTN signal to check for aggregation.</param>
    /// <param name="settings">Tributary slot settings, provided by <see cref="IOtnNode"/></param>
    /// <returns><c>true</c> if the client can be aggregated; otherwise, <c>false</c>.</returns>
    bool CanAggregate(IOtnSignal client, IOtnSettings settings);
    /// <summary>
    /// Determines whether a client OTN signal can be aggregated within this container OTN signal.
    /// </summary>
    /// <param name="client">The client signal to check for aggregation.</param>
    /// <param name="settings">Tributary slot settings, provided by <see cref="IOtnNode"/></param>
    /// <param name="otnClient">The Otn client signal (if there's mapping)</param>
    /// <returns><c>true</c> if the client can be aggregated; otherwise, <c>false</c>.</returns>
    bool TryAggregate(IOtnSignal client, IOtnSettings settings);
    /// <summary>
    /// Attempts to de-aggregate a client signal from this OTN signal container.
    /// </summary>
    /// <param name="client">The client signal to de-aggregate.</param>
    /// <returns><c>true</c> if de-aggregation is successful; otherwise, <c>false</c>.</returns>
    bool TryDeAggregate(IOtnSignal client);
}