using OTN.Enums;
using QuikGraph;
using System.Collections.Generic;

namespace OTN.Interfaces;

/// <summary>
/// Represents an Optical Transport Network (OTN) signal with a specific OTU/ODU level, 
/// supporting client signal aggregation.
/// </summary>
public interface IOtnSignal : IEdge<IOtnNode>, ISignal
{
    /// <summary>
    /// LO and intermediate OTN signals
    /// </summary>
    IReadOnlyList<IOtnSignal> Aggregation { get; }
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
    bool CanAggregate(ISignal client, IOtnSettings settings, out IOtnSignal otnClient);
    /// <summary>
    /// Attempts to aggregate a client signal within this OTN signal container.
    /// </summary>
    /// <param name="client">The client OTN signal to aggregate.</param>
    /// <param name="settings">Tributary slot settings, provided by <see cref="IOtnNode"/></param>
    /// <returns><c>true</c> if aggregation is successful; otherwise, <c>false</c>.</returns>
    bool TryAggregate(ISignal client, IOtnSettings settings);
}