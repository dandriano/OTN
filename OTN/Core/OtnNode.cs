using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OTN.Core;

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
public delegate bool AggregationSelector(IEnumerable<IOtnSignal> signals, IOtnSignal client, [NotNullWhen(true)] out IOtnSignal? selectedContainer);

/// <inheritdoc />
public class OtnNode : IOtnNode
{
    private readonly IOtnSettings _settings;
    private readonly List<AggregationRule> _rules = new List<AggregationRule>();
    private readonly List<IOtnSignal> _signals;
    /// <inheritdoc />
    public IReadOnlyList<IOtnSignal> Signals => _signals.AsReadOnly();
    /// <inheritdoc />

    /// <summary>
    /// Initializes a new instance of the <see cref="OtnNode"/> class 
    /// with aggregation rules and optional initial capacity.
    /// </summary>
    /// <param name="rules">The aggregation rules defining allowed client-to-container aggregations.</param>
    /// <param name="capacity">Initial capacity to allocate for storing HO OTN signals.</param>
    /// <exception cref="InvalidOperationException">Thrown if the rules do not support transitive aggregation up to the maximum container level.</exception>
    /// <remarks>
    /// Validates aggregation rules upon initialization to ensure 
    /// transitive aggregation support up to the highest container level.
    /// </remarks>
    public OtnNode(IEnumerable<AggregationRule> rules, int capacity = 1) : this(rules, OtnSettings.Default, capacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OtnNode"/> class 
    /// with aggregation rules and optional initial capacity.
    /// </summary>
    /// <param name="rules">The aggregation rules defining allowed client-to-container aggregations.</param>
    /// <param name="settings">Tributary slot count settings</param>
    /// <param name="capacity">Initial capacity to allocate for storing HO OTN signals.</param>
    /// <exception cref="InvalidOperationException">Thrown if the rules do not support transitive aggregation up to the maximum container level.</exception>
    /// <remarks>
    /// Validates aggregation rules upon initialization to ensure 
    /// transitive aggregation support up to the highest container level.
    /// </remarks>
    public OtnNode(IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1)
    {
        _settings = settings;
        _rules = rules.ToList();

        // Validate rules upfront and populate cache
        var maxRule = _rules.MaxBy(r => r.ContainerType)?.ContainerType
            ?? throw new InvalidOperationException("There's no rules");

        foreach (var rule in _rules.Select(r => r.ClientType)
                                   .Distinct())
        {
            if (!IsAggregationSupportedTransitive(rule, maxRule, out _))
                throw new InvalidOperationException("Invalid rules");
        }

        foreach (var rule in _rules.Where(r => r.ContainerType != maxRule)
                                   .Select(r => r.ContainerType)
                                   .Distinct())
        {
            if (!IsAggregationSupportedTransitive(rule, maxRule, out _))
                throw new InvalidOperationException("Invalid rules");
        }

        _signals = new List<IOtnSignal>(capacity);
    }

    /// <inheritdoc />
    public bool TryAggregate(IOtnSignal client, AggregationStrategy strategy = AggregationStrategy.NextFit)
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


    /// <inheritdoc />
    public bool TryAggregate(IOtnSignal client, AggregationSelector selector)
    {
        if (selector(_signals, client, out var selectedContainer))
            return selectedContainer.TryAggregate(client, _settings);

        // Assume we have path to the highest OTN level rule
        var targetLevel = _rules.Select(r => r.ContainerType).Max();
        if (!IsAggregationSupportedTransitive(client.OduLevel, targetLevel, out var containerLevel))
            return false;

        // Create new container signal to hold client
        var newContainer = new OtnSignal(Enum.GetName(containerLevel!.Value)!, containerLevel.Value.ExpectedBandwidthGbps(), containerLevel.Value);
        if (newContainer.TryAggregate(client, _settings))
        {
            if (TryAggregate(newContainer, selector))
            {
                return true;
            }
            else if (_signals.Capacity > _signals.Count)
            {
                _signals.Add(newContainer);
                return true;
            }
        }

        return false;
    }


    /// <inheritdoc />
    public bool IsAggregationSupported(OtnLevel client, OtnLevel container)
    {
        // Strict hierarchy check
        if ((int)client >= (int)container)
            return false;

        // Direct rule check
        return _rules.Any(r => r.ClientType == client && r.ContainerType == container);
    }

    /// <inheritdoc />
    public bool IsAggregationSupportedTransitive(OtnLevel client, OtnLevel container, [NotNullWhen(true)] out OtnLevel? foundNextHop)
    {
        foundNextHop = null;

        if ((int)client >= (int)container)
            return false;

        var visited = new HashSet<OtnLevel>();
        return IsAggregationSupportedRecursive(client, container, visited, out foundNextHop);
    }

    private bool IsAggregationSupportedRecursive(OtnLevel current, OtnLevel target, HashSet<OtnLevel> visited, [NotNullWhen(true)] out OtnLevel? foundNextHop)
    {
        foundNextHop = null;

        if (visited.Contains(current))
            return false;
        visited.Add(current);

        if (IsAggregationSupported(current, target))
        {
            foundNextHop = target;
            return true;
        }

        var nextLevels = _rules.Where(r => r.ClientType == current).Select(r => r.ContainerType);
        foreach (var next in nextLevels.OrderDescending())
        {
            // Inefficiency, could gather a path/cache
            if (IsAggregationSupportedRecursive(next, target, visited, out _))
            {
                foundNextHop = next;
                return true;
            }
        }
        return false;
    }

    private bool NextFitSelector(IEnumerable<IOtnSignal> signals, IOtnSignal client, [NotNullWhen(true)] out IOtnSignal? selectedContainer)
    {
        selectedContainer = null;
        foreach (var container in signals.Reverse())
        {
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client, _settings, out _))
            {
                selectedContainer = container;
                return true;
            }
        }
        return false;
    }

    private bool FirstFitSelector(IEnumerable<IOtnSignal> signals, IOtnSignal client, [NotNullWhen(true)] out IOtnSignal? selectedContainer)
    {
        selectedContainer = null;
        foreach (var container in signals)
        {
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client, _settings, out _))
            {
                selectedContainer = container;
                return true;
            }
        }
        return false;
    }

    private bool BestFitSelector(IEnumerable<IOtnSignal> signals, IOtnSignal client, [NotNullWhen(true)] out IOtnSignal? selectedContainer)
    {
        selectedContainer = null;
        int minRemaining = int.MaxValue;

        foreach (var container in signals)
        {
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client, _settings, out _))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Aggregation.Sum(c => _settings.SlotsRequired(c.OduLevel));
            int clientSlots = _settings.SlotsRequired(client.OduLevel);
            int remaining = _settings.SlotsAvailable(container.OduLevel) - (currentUsedSlots + clientSlots);

            if (remaining < minRemaining)
            {
                selectedContainer = container;
                minRemaining = remaining;
            }
        }

        return selectedContainer != null;
    }

    private bool WorstFitSelector(IEnumerable<IOtnSignal> signals, IOtnSignal client, [NotNullWhen(true)] out IOtnSignal? selectedContainer)
    {
        selectedContainer = null;
        int maxRemaining = int.MinValue;

        foreach (var container in signals)
        {
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client, _settings))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Aggregation.Sum(c => _settings.SlotsRequired(c.OduLevel));
            int clientSlots = _settings.SlotsRequired(client.OduLevel);
            int remaining = _settings.SlotsAvailable(container.OduLevel) - (currentUsedSlots + clientSlots);

            if (remaining > maxRemaining)
            {
                selectedContainer = container;
                maxRemaining = remaining;
            }
        }

        return selectedContainer != null;
    }
}