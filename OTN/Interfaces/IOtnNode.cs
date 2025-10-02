using OTN.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OTN.Interfaces;

/// <summary>
/// Represents an OTN node which manages aggregation rules and signals.
/// </summary>
/// <remarks>
/// This is abstracted from circuit pack/equipment node
/// </remarks>
public interface IOtnNode
{
    /// <summary>
    /// HO OTN signals
    /// </summary>
    IReadOnlyList<IOtnSignal> Signals { get; }
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
    /// <summary>
    /// Attempts to add a client OTN signal to this node
    /// The signal is assigned to the last suitable container signal if possible; otherwise, a new container is created.
    /// </summary>
    /// <remarks>
    /// Supports multiple bin packing heuristics: NextFit (default), FirstFit, BestFit, WorstFit.
    /// </remarks>
    /// <param name="client">The client OTN signal to add.</param>
    /// <param name="strategy">The aggregation/bin packing strategy to use. Defaults to NextFit.</param>
    /// <returns><c>true</c> if the client signal was successfully aggregated; otherwise, <c>false</c>.</returns>
    bool TryAggregate(IOtnSignal client, AggregationStrategy strategy = AggregationStrategy.NextFit);
    /// <summary>
    /// Attempts to add a client OTN signal to this node
    /// The signal is assigned to the last suitable container signal if possible; otherwise, a new container is created.
    /// </summary>
    /// <param name="client">The client OTN signal to add.</param>
    /// <param name="selector">The delegate to select a fitting container.</param>
    /// <returns><c>true</c> if the client signal was successfully aggregated; otherwise, <c>false</c>.</returns>
    bool TryAggregate(IOtnSignal client, AggregationSelector selector);
}