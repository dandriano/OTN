using OTN.Enums;
using QuikGraph;
using System.Collections.Generic;

namespace OTN.Interfaces;

/// <summary>
/// Represents an Optical Transport Network (OTN) signal with a specific OTU/ODU level, 
/// supporting client signal aggregation.
/// </summary>
public interface IOtnSignal : ISignal, IEdge<IOtnNode>
{
    /// <summary>
    /// LO and intermediate OTN signal aggregation
    /// </summary>
    IEnumerable<IOtnSignal> Signals { get; }
    int SignalCount { get; }
    OtnLevel OduLevel { get; }
}