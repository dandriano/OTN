using System.Collections.Generic;
using OTN.Enums;
using QuikGraph;

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