using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OTN.Enums;
using OTN.Extensions;
using OTN.Interfaces;

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
public delegate bool AggregationSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer);


/// <summary>
/// The Otn Node class represents a node in an OTN (Optical Transport Network) 
/// that manages higher-order (HO) OTN signals and handles their aggregation based on predefined rules. 
/// It acts as a container for top-level signals and enforces aggregation logic 
/// using bin-packing-inspired strategies. 
/// The class validates rules during initialization 
/// to ensure transitive aggregation is possible up to the maximum container level.
/// <inheritdoc />
/// </summary>
public class OtnNode : IOtnNode
{
    private readonly int _capacity;
    private readonly IOtnSettings _settings;
    private readonly List<AggregationRule> _rules = new List<AggregationRule>();
    private readonly Dictionary<Guid, OtnSignal> _signals = new Dictionary<Guid, OtnSignal>();

    public Guid Id { get; } = Guid.NewGuid();
    public INetNode NetNode { get; }
    /// <inheritdoc />
    public IEnumerable<IOtnSignal> Signals => _signals.Values;
    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of the <see cref="OtnNode"/> class 
    /// with aggregation rules and optional initial capacity.
    /// </summary>
    /// <param name="node">The parent node, which hold this node</param>
    /// <param name="rules">The aggregation rules defining allowed client-to-container aggregations.</param>
    /// <param name="capacity">Initial capacity to allocate for storing HO OTN signals.</param>
    /// <exception cref="InvalidOperationException">Thrown if the rules do not support transitive aggregation up to the maximum container level.</exception>
    /// <remarks>
    /// Validates aggregation rules upon initialization to ensure 
    /// transitive aggregation support up to the highest container level.
    /// </remarks>
    public OtnNode(INetNode node, IEnumerable<AggregationRule> rules, int capacity = 1) :
        this(node, rules, OtnSettings.Default, capacity)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OtnNode"/> class 
    /// with aggregation rules and optional initial capacity.
    /// </summary>
    /// <param name="node">The parent node, which hold this node</param>
    /// <param name="rules">The aggregation rules defining allowed client-to-container aggregations.</param>
    /// <param name="settings">Tributary slot count settings</param>
    /// <param name="capacity">Initial capacity to allocate for storing HO OTN signals.</param>
    /// <exception cref="InvalidOperationException">Thrown if the rules do not support transitive aggregation up to the maximum container level.</exception>
    /// <remarks>
    /// Validates aggregation rules upon initialization to ensure 
    /// transitive aggregation support up to the highest container level.
    /// </remarks>
    public OtnNode(INetNode node, IEnumerable<AggregationRule> rules, IOtnSettings settings, int capacity = 1)
    {
        NetNode = node;
        _capacity = capacity;
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
    }

    /// <inheritdoc />
    public bool TryAggregate(OtnSignal client, [NotNullWhen(true)] out OtnSignal? aggregated, AggregationStrategy strategy = AggregationStrategy.NextFit, bool dontCreateAggregated = false)
    {
        AggregationSelector selector = strategy switch
        {
            AggregationStrategy.NextFit => NextFitSelector,
            AggregationStrategy.FirstFit => FirstFitSelector,
            AggregationStrategy.BestFit => BestFitSelector,
            AggregationStrategy.WorstFit => WorstFitSelector,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy))
        };

        return TryAggregate(client, out aggregated, selector, dontCreateAggregated);
    }

    /// <summary>
    /// Attempts to place a LO client signal into an appropriate HO container signal, potentially creating 
    /// nested HO containers recursively. Returns true if successful, with aggregated set to 
    /// the top-level HO container the client ended up in (or the client itself if it's already top-level). 
    /// Can false due to lack of rules, no available slots, or capacity limits at the top level.
    /// </summary>
    /// <param name="client">The client OTN signal to add.</param>
    /// <param name="aggregated">The aggregation result</param>
    /// <param name="selector">The delegate to select a fitting container.</param>
    /// <param name="dontCreateAggregated">The flag to limit aggregation to existing container signals.</param>
    /// <returns><c>true</c> if the client signal was successfully aggregated; otherwise, <c>false</c>.</returns>
    public bool TryAggregate(OtnSignal client, [NotNullWhen(true)] out OtnSignal? aggregated, AggregationSelector selector, bool dontCreateAggregated = false)
    {
        aggregated = null;
        var targetLevel = _rules.Select(r => r.ContainerType).Max();
        // Aggregating another HO signal?
        if (targetLevel == client.OduLevel && client.SignalCount != 0)
        {
            if (_signals.TryGetValue(client.Id, out aggregated))
            {
                // Is it already there?
                return true;
            }
            else if (_capacity > _signals.Count)
            {
                aggregated = client;
                _signals.Add(aggregated.Id, aggregated);
                return true;
            }
            return false;
        }

        if (selector(_signals.Values, client, out var existedContainer) && existedContainer.TryAggregate(client, _settings))
        {
            // There's container to fill
            aggregated = existedContainer;
            return true;
        }
        // Assume we have path to the highest OTN level rule
        if (!IsAggregationSupportedTransitive(client.OduLevel, targetLevel, out var containerLevel) || dontCreateAggregated)
            return false;

        // Create new container signal to hold client
        var newContainer = new OtnSignal(client.Source,
                                         client.Target,
                                         containerLevel.Value.ExpectedBandwidthGbps(),
                                         containerLevel.Value);
        // Let's try to aggregate recursively
        if (newContainer.TryAggregate(client, _settings)
            && TryAggregate(newContainer, out aggregated, selector))
        {
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool TryDeAggregate(OtnSignal client, [NotNullWhen(true)] out OtnSignal? deAggregated)
    {
        deAggregated = null;
        if (_signals.Remove(client.Id))
        {
            deAggregated = client;
            return true;
        }

        foreach (var ho in _signals.Values.ToList())
        {
            if (ho.TryDeAggregate(client))
            {
                if (ho.SignalCount == 0)
                    return TryDeAggregate(ho, out deAggregated);
                deAggregated = ho;
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

    private bool NextFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        foreach (var container in signals.Reverse())
        {
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client, _settings))
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
            if (IsAggregationSupported(client.OduLevel, container.OduLevel) && container.CanAggregate(client, _settings))
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
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client, _settings))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Signals.Sum(c => _settings.SlotsRequired(c.OduLevel));
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

    private bool WorstFitSelector(IEnumerable<OtnSignal> signals, OtnSignal client, [NotNullWhen(true)] out OtnSignal? selectedContainer)
    {
        selectedContainer = null;
        int maxRemaining = int.MinValue;

        foreach (var container in signals)
        {
            if (!IsAggregationSupported(client.OduLevel, container.OduLevel) || !container.CanAggregate(client, _settings))
                continue;

            // Calculate the remaining slots if the client were added.
            int currentUsedSlots = container.Signals.Sum(c => _settings.SlotsRequired(c.OduLevel));
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