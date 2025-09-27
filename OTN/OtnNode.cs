using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN;

public record AggregationRule
{
    public OtnLevel ClientType { get; }
    public OtnLevel ContainerType { get; }

    public AggregationRule(OtnLevel clientType, OtnLevel containerType)
    {
        ClientType = clientType;
        ContainerType = containerType;
    }
}

public class OtnNode
{
    private readonly List<AggregationRule> _rules = [];
    private readonly List<OtnSignal> _signals = [];

    public OtnNode(IEnumerable<AggregationRule> rules)
    {
        _rules = [.. rules];
    }

    public bool IsAggregationSupported(OtnLevel client, OtnLevel container)
    {
        // Strict hierarchy check
        if ((int)client >= (int)container)
            return false;

        // Direct rule check
        if (_rules.Any(r => r.ClientType == client && r.ContainerType == container))
            return true;
        return false;

        // Transitive rule check (in reality doesnt needed)
        /*
        var visitor = new HashSet<OduLevel>();
        Func<OduLevel, OduLevel, HashSet<OduLevel>, bool> transitiveRecursiveRuleCheck = null!;
        transitiveRecursiveRuleCheck = (current, target, visited) =>
        {
            if (visited.Contains(current))
                return false;
            visited.Add(current);

            foreach (var intermediate in _rules.Where(r => r.ClientType == client)
                                               .Select(r => r.ContainerType)
                                               .Distinct())
            {
                if ((int)intermediate <= (int)current)
                    continue;

                if (intermediate == target)
                    return true;

                if (transitiveRecursiveRuleCheck(intermediate, target, visited))
                    return true;
            }

            return false;
        };

        return transitiveRecursiveRuleCheck(client, container, visitor);
        */
    }
}