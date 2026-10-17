using System;
using System.Collections.Generic;
using System.Collections.Frozen;

namespace OTN.Core;

/// <summary>Required and avoided nodes for one route search.</summary>
public sealed class RouteConstraints
{
    public static RouteConstraints Empty { get; } = new RouteConstraints();

    public IReadOnlySet<NetNode> RequiredNodes { get; }
    public IReadOnlySet<NetNode> AvoidedNodes { get; }

    public RouteConstraints(
        IEnumerable<NetNode>? requiredNodes = null,
        IEnumerable<NetNode>? avoidedNodes = null)
    {
        var required = (requiredNodes ?? []).ToFrozenSet();
        var avoided = (avoidedNodes ?? []).ToFrozenSet();

        foreach (var node in required)
            ArgumentNullException.ThrowIfNull(node);
        foreach (var node in avoided)
            ArgumentNullException.ThrowIfNull(node);

        if (required.Overlaps(avoided))
            throw new ArgumentException("A node cannot be both required and avoided.");

        RequiredNodes = required;
        AvoidedNodes = avoided;
    }
}
