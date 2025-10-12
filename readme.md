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

**TODO**: Write more about Dijkstra's and Yen's algos, and also about the TSP poroblem -- for example, how must-pass routing is a subset of TSP (and simplest NN heuristic as solution). Also, mention the paper "A (Slightly) Improved Approximation Algorithm for Metric TSP" by Anna R. Karlin.