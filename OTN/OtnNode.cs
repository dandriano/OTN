using OTN.Enums;
using OTN.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OTN;

/// <summary>
/// Represents a rule defining which client OTN level can be aggregated into which container OTN level.
/// </summary>
/// <param name="ClientType">The OTN level of the client signal.</param>
/// <param name="ContainerType">The OTN level of the container signal.</param>
public record AggregationRule(OtnLevel ClientType, OtnLevel ContainerType);

/// <summary>
/// Delegate for selecting a container signal based on a bin packing strategy.
/// </summary>
/// <param name="signals">The available container signals.</param>
/// <param name="client">The client signal to aggregate.</param>
/// <param name="selectedContainer">When this method returns <c>true</c>, contains the selected container; otherwise, <c>null</c>.</param>
/// <returns><c>true</c> if a fitting container was found; otherwise, <c>false</c>.</returns>
public delegate bool AggregationSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer);

/// <summary>
/// Represents an OTN node which manages aggregation rules and signals.
/// </summary>
/// <remarks>
/// This is abstracted from circuit pack/equipment node
/// </remarks>
public class OtnNode
{
    private readonly List<AggregationRule> _rules = new List<AggregationRule>();
    private readonly List<OtnSignal> _signals = new List<OtnSignal>();
    public IReadOnlyList<OtnSignal> Signals => _signals.AsReadOnly();

    public OtnNode(IEnumerable<AggregationRule> rules)
    {
        _rules = rules.ToList();
    }
    
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
    public bool TryAggregate(OtnSignal client, AggregationStrategy strategy = AggregationStrategy.NextFit)
    {
        AggregationSelector selector = strategy switch
        {
            AggregationStrategy.NextFit => NextFitSelector,
            AggregationStrategy.FirstFit => FirstFitSelector,
            AggregationStrategy.BestFit => BestFitSelector,
            AggregationStrategy.WorstFit => WorstFitSelector,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy))
        };

        return TryAggregate(client, selector);
    }

    /// <summary>
    /// Attempts to add a client OTN signal to this node
    /// The signal is assigned to the last suitable container signal if possible; otherwise, a new container is created.
    /// </summary>
    /// <param name="client">The client OTN signal to add.</param>
    /// <param name="selector">The delegate to select a fitting container.</param>
    /// <returns><c>true</c> if the client signal was successfully aggregated; otherwise, <c>false</c>.</returns>
    public bool TryAggregate(OtnSignal client, AggregationSelector selector)
    {
        if (selector(_signals, client, out var selectedContainer))
            return selectedContainer.TryAggregate(client);

        // Assume we have path to the highest OTN level rule
        var targetLevel = _rules.Select(r => r.ContainerType).Max();
        if (!IsAggregationSupportedTransitive(client.OduLevel, targetLevel, out var containerLevel))
            return false;

        // Create new container signal to hold client
        var newContainer = new OtnSignal(Guid.NewGuid(), Enum.GetName(containerLevel.Value)!, containerLevel.Value.ExpectedBandwidthGbps(), containerLevel.Value);
        if (newContainer.TryAggregate(client))
        {
            if (!TryAggregate(newContainer, selector))
                _signals.Add(newContainer);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether aggregation is supported for a given client and container OTN level based on the node's rules.
    /// </summary>
    /// <param name="client">The client OTN level.</param>
    /// <param name="container">The container OTN level.</param>
    /// <returns><c>true</c> if aggregation is supported; otherwise <c>false</c>.</returns>
    public bool IsAggregationSupported(OtnLevel client, OtnLevel container)
    {
        // Strict hierarchy check
        if ((int)client >= (int)container)
            return false;

        // Direct rule check
        return _rules.Any(r => r.ClientType == client && r.ContainerType == container);
    }

    /// <summary>
    /// Determines if aggregation is supported directly or transitively 
    /// between the given client and container OTN levels.
    /// </summary>
    /// <param name="client">The OTN level of the client signal.</param>
    /// <param name="container">The OTN level of the container signal.</param>
    /// <param name="foundIntermediate">
    /// When this method returns, contains the intermediate OTN level found 
    /// during the transitive aggregation check that supports the aggregation. 
    /// This will be default if no such level is found.
    /// </param>
    /// <returns>
    /// <c>true</c> if aggregation is supported either directly or 
    /// through one or more intermediate levels; otherwise, <c>false</c>.
    /// </returns>
    public bool IsAggregationSupportedTransitive(OtnLevel client, OtnLevel container, [NotNullWhen(true)] out OtnLevel? foundIntermediate)
    {
        foundIntermediate = null;

        if ((int)client >= (int)container)
            return false;

        var visited = new HashSet<OtnLevel>();
        return IsAggregationSupportedRecursive(client, container, visited, out foundIntermediate);
    }

    private bool IsAggregationSupportedRecursive(OtnLevel current, OtnLevel target, HashSet<OtnLevel> visited, [NotNullWhen(true)] out OtnLevel? foundIntermediate)
    {
        foundIntermediate = null;

        if (visited.Contains(current))
            return false;
        visited.Add(current);

        if (IsAggregationSupported(current, target))
        {
            foundIntermediate = target;
            return true;
        }

        var nextLevels = _rules.Where(r => r.ClientType == current).Select(r => r.ContainerType);
        foreach (var next in nextLevels.OrderDescending())
        {
            if (IsAggregationSupportedRecursive(next, target, visited, out foundIntermediate))
                return true;
        }
        return false;
    }

    private bool NextFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        foreach (var container in signals.Reverse())
        {
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client))
            {
                selectedContainer = container;
                return true;
            }
        }
        return false;
    }

    private bool FirstFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        foreach (var container in signals)
        {
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client))
            {
                selectedContainer = container;
                return true;
            }
        }
        return false;
    }

    private bool BestFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        int minRemaining = int.MaxValue;

        foreach (var container in signals)
        {
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Aggregation.Sum(c => c.OduLevel.SlotsRequired());
            int clientSlots = client.OduLevel.SlotsRequired();
            int remaining = container.OduLevel.SlotsAvailable() - (currentUsedSlots + clientSlots);

            if (remaining < minRemaining)
            {
                selectedContainer = container;
                minRemaining = remaining;
            }
        }

        return selectedContainer != null;
    }

    private bool WorstFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        int maxRemaining = int.MinValue;

        foreach (var container in signals)
        {
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Aggregation.Sum(c => c.OduLevel.SlotsRequired());
            int clientSlots = client.OduLevel.SlotsRequired();
            int remaining = container.OduLevel.SlotsAvailable() - (currentUsedSlots + clientSlots);

            if (remaining > maxRemaining)
            {
                selectedContainer = container;
                maxRemaining = remaining;
            }
        }

        return selectedContainer != null;
    }
}