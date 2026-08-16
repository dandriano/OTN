# OTN

Aggregation/multiplexing and routing research

## Trivia

### Aggregation/multiplexing

The [`OtnNode`](/OTN/Core/OtnNode.cs), [`OtnSignal`](/OTN/Core/OtnSignal.cs), and [`AggregationRule`](/OTN/Core/OtnNode.cs) classes address the challenge of managing signal aggregation and deaggregation in Optical Transport Networks (OTN). They provide a simplified model for multiplexing lower-order signals into higher-order containers while enforcing capacity constraints. This model abstracts real equipment to enable seamless signal organization and rule validation in a network node. Given a requirements matrix, these classes enable automated signal (de)aggregation, streamlining the process for the end user of a hypothetical CAD application. Additionally, the [`IOtnSettings`](/OTN/Interfaces/IOtnSettings.cs) interface (a contract and default implementation) represents OTN hierarchy rules based on the concept of "tributary slots":

```text
ODU4 ────────────────────────────────────────────────┐
|                                                    │
├─ 80 × ODU0 ────────────────────────────────────────┘
│
├─ 40 × ODU1 ─┬─ 2 × ODU0 ───────────────────────────┘
│
├─ 10 × ODU2 ─┬─ 4 × ODU1 ─┬─ 2 × ODU0 ──────────────┘
│             │
│             └─ 8 × ODU0 ───────────────────────────┘
│
└─ 2 × ODU3 ─┬─ 4 × ODU2 ─┬─ 4 × ODU1 ─┬─ 2 × ODU0 ──┘
             │            │
             │            └─ 8 × ODU0 ───────────────┘
             │
             ├─ 16 × ODU1 ─┬─ 2 × ODU0 ──────────────┘
             │
             └─ 32 × ODU0 ───────────────────────────┘
```

**TODO**: Write more about G.709, perhaps. Also, [`AggregationStrategy`](/OTN/Enums/AggregationStrategy.cs) which are based on [online algos](https://en.wikipedia.org/wiki/Online_algorithm) such Next/First/Best/Worst- bin packing.

### Routing

Routing algorithms such as Dijkstra's and Yen's address shortest-path problems in networks, with Yen's algorithm extending Dijkstra's to find k-shortest paths. They are old but effective.

The [`NetNode`](/OTN/Core/NetNode.cs) represents a real location with typical DWDM equipment, and the [`Link`](/OTN/Core/Link.cs) represents an optical fiber span. Both layers (electrical `OtnNode`/`OtnSignal` and optical `NetNode`/`Link`) can be routed through as weighted (un)directed graphs.

## High-level architecture

### 1) OTN aggregation layer (OTN/Core/*)

Key types:

- OtnLevel (ODU0..ODU4): the hierarchy levels.
- IOtnSettings / OtnSettings.Default: defines the slot model:
    - SlotsRequired(level) (how many “tributary slots” a signal at that level consumes)
    - SlotsAvailable(level) (capacity per container level)

- Signal (OTN/Core/Signal.cs): a generic signal between two OtnNodes with a bandwidth.
- OtnSignal (OTN/Core/OtnSignal.cs): an OTN-level signal that can contain other signals:
    - Holds an internal _aggregation map {clientSignalId -> clientSignal}.
    - CanAggregate(IOtnSignal client, IOtnSettings settings) checks:
        - hierarchy (client must be lower than container),
        - bandwidth matches expected bandwidth per level (within tolerance),
        - slot capacity constraint:
        currentUsedSlots + clientSlots <= SlotsAvailable(containerLevel).

    - TryAggregate adds the client to the internal aggregation.
    - TryDeAggregate removes directly or recursively from nested containers.

- AggregationRule (OTN/Core/OtnNode.cs): (ClientType -> ContainerType) allowed direct aggregation.
- OtnNode (OTN/Core/OtnNode.cs): the aggregation “equipment” at a site:
    - Has a parent INetNode NetNode.
    - Maintains a set of top-level HO OtnSignals in _signals.
    - Constructor validates that the rule graph supports transitive aggregation up to the maximum container level (so nested aggregation paths can be formed).

    - TryAggregate(...) is the main entry point:
        - If the client is already at the max/container level, it either reuses an existing container by Id or stores it if _capacity allows.
        - Otherwise, it selects an existing container candidate using a bin-packing-inspired selector:
        NextFit, FirstFit, BestFit, WorstFit (AggregationStrategy).

        - If no existing container fits (or dontCreateAggregated=true), it checks whether transitive aggregation is possible and, if so, creates a new
        intermediate container and recursively aggregates.

    - TryDeAggregate removes a LO client from the node’s hierarchy and “bubbles up” if intermediate containers become empty.

### 2) Optical routing layer (OTN/Core/* + OTN/Extensions/RoutingExtensions.cs)

Key types:

- Network (OTN/Core/Network.cs):
    - Optical: BidirectionalGraph<NetNode, Link> for routing
    - Electrical: BidirectionalGraph<OtnNode, Signal> for aggregation connectivity
    - Provides helpers like:
        - AddNetNode, AddLink
        - AddOtnNode
        - AddSignal(...) which auto-creates OTN aggregation by converting a Signal to an OtnSignal (SignalExtensions) and then aggregating at source/target OtnNodes.

- NetNode (OTN/Core/NetNode.cs): a graph vertex with:
    - NetNodeType (Terminal / OLA / ROADM)
    - RouteNodeType (InRoute / OutRoute), used by “must-pass” routing.

- Link (OTN/Core/Link.cs): a weighted edge between NetNodes.
    - Supports LinkType with optional Reverse wiring.

- RoutingExtensions (OTN/Extensions/RoutingExtensions.cs):
    - FindOpticPath(graph, source, target, k=5):
        - Computes up to k shortest paths using a Dijkstra-based initial shortest path and then a loop that resembles Yen’s k-shortest paths (spur/root
        decomposition, temporarily “blocking” edges/nodes by returning Infinity weights).

        - It filters candidates to keep path uniqueness via a signature (node id sequence).

    - FindMustPassOpticPath(graph, source, target):
        - Builds a restricted set of RouteNodeType.InRoute nodes.
        - Uses a Nearest Neighbor heuristic guided by repeated Dijkstra shortest path distances between the current node and remaining must-pass nodes.

### 3) Signal conversion helpers

- SignalExtensions (OTN/Extensions/SignalExtensions.cs):
    - ToOtnSignal() picks the minimal ODU level whose expected bandwidth is >= the signal’s bandwidth (within tolerance).
    - TryToOtnSignal(signal, oduLevel) checks bandwidth constraints and constructs the OtnSignal.