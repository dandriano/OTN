using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OTN.Enums;

namespace OTN.Interfaces;

/// <summary>
/// Represents an OTN node which manages aggregation rules and signals.
/// </summary>
/// <remarks>
/// This is abstracted from circuit pack/equipment node
/// </remarks>
public interface IOtnNode
{
    Guid Id { get; }
    INetNode NetNode { get; }
    /// <summary>
    /// HO OTN signals
    /// </summary>
    IEnumerable<IOtnSignal> Signals { get; }
    /// <summary>
    /// Determines whether aggregation is supported for a given client and container OTN level based on the node's rules.
    /// </summary>
    /// <param name="client">The client OTN level.</param>
    /// <param name="container">The container OTN level.</param>
    /// <returns><c>true</c> if aggregation is supported; otherwise <c>false</c>.</returns>
    bool IsAggregationSupported(OtnLevel client, OtnLevel container);
    /// <summary>
    /// Determines if aggregation is supported directly or transitively 
    /// between the given client and container OTN levels.
    /// </summary>
    /// <param name="client">The OTN level of the client signal.</param>
    /// <param name="container">The OTN level of the container signal.</param>
    /// <param name="foundNextHop">
    /// When this method returns, contains the intermediate (next hop) OTN level found 
    /// during the transitive aggregation check that supports the aggregation 
    /// (so not so efficient, think about cache or something). 
    /// This will be default if no such level is found.
    /// </param>
    /// <returns>
    /// <c>true</c> if aggregation is supported either directly or 
    /// through one or more intermediate levels; otherwise, <c>false</c>.
    /// </returns>
    bool IsAggregationSupportedTransitive(OtnLevel client, OtnLevel container, [NotNullWhen(true)] out OtnLevel? foundNextHop);
}