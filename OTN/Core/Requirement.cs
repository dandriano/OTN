using OTN.Interfaces;

namespace OTN.Core;

public class Requirement<T, TWeight>  // maybe ITaggedEdge<NetNode, double> or ITaggedEdge<OtnNode, int>
    where T : INetNode
    where TWeight : struct
{
    public T Source { get; }
    public T Target { get; }
    public TWeight Tag { get; }
    public int TagCount { get; }
    public string Notes { get; }

    public Requirement(T source, T target, TWeight weight, int weightCount = 2, string notes = "")
    {
        Source = source;
        Target = target;
        Tag = weight;
        TagCount = weightCount;
        Notes = notes;
    }
}