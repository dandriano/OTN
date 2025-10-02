namespace OTN.Interfaces;

/// <summary>
/// Represents a generic (mostly client) signal with associated bandwidth.
/// </summary>
public interface ISignal
{
    double BandwidthGbps { get; }
}