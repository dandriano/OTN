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
/// Represents an OTN node which manages aggregation rules and signals.
/// </summary>
/// <remarks>
/// This is abstracted from circuit pack/equipment node
/// </remarks>
public class OtnNode
{
    private readonly List<AggregationRule> _rules = [];
    private readonly List<OtnSignal> _signals = [];

    public OtnNode(IEnumerable<AggregationRule> rules)
    {
        _rules = [.. rules];
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
        foundIntermediate = default;

        if ((int)client >= (int)container)
            return false;

        var visited = new HashSet<OtnLevel>();
        return IsAggregationSupportedRecursive(client, container, visited, out foundIntermediate);
    }

    private bool IsAggregationSupportedRecursive(OtnLevel current, OtnLevel target, HashSet<OtnLevel> visited, [NotNullWhen(true)] out OtnLevel? foundIntermediate)
    {
        foundIntermediate = default;

        if (visited.Contains(current))
            return false;
        visited.Add(current);

        if (_rules.Any(r => r.ClientType == current && r.ContainerType == target))
        {
            foundIntermediate = target;
            return true;
        }

        var nextLevels = _rules.Where(r => r.ClientType == current).Select(r => r.ContainerType);
        foreach (var next in nextLevels)
        {
            if ((int)next <= (int)current)
                continue;

            if (IsAggregationSupportedRecursive(next, target, visited, out foundIntermediate))
                return true;
        }
        return false;
    }
}