using OTN.Interfaces;

namespace OTN.Core;

public class Requirement<T> where T : INetNode
{
    public T Source { get; }
    public T Target { get; }
    public double BandwidthGbps { get; }
    public int FiberCount { get; }
    public string Notes { get; }

    public Requirement(T source, T target, double bandwidthGbps, int fiberCount = 2, string notes = "")
    {
        Source = source;
        Target = target;
        BandwidthGbps = bandwidthGbps;
        FiberCount = fiberCount;
        Notes = notes;
    }
}