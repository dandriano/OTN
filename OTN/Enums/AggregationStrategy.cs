namespace OTN.Enums;

/// <summary>
/// Enum for bin packing strategies used in signal aggregation.
/// </summary>
public enum AggregationStrategy
{
    /// <summary>
    /// Tries to fit into the most recently added containers first.
    /// </summary>
    NextFit,

    /// <summary>
    /// Tries to fit into the earliest added containers first.
    /// </summary>
    FirstFit,

    /// <summary>
    /// Selects the container that leaves the least remaining slots after adding.
    /// </summary>
    BestFit,

    /// <summary>
    /// Selects the container that leaves the most remaining slots after adding.
    /// </summary>
    WorstFit
}