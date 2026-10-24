using OTN.Core;

namespace OTN.Wasm.ViewModels;

/// <summary>Canvas-only state associated with a network node.</summary>
public sealed class NodeViewModel(NetNode node, double x, double y)
{
    public string Id { get; } = node.Id.ToString()[..5];
    public NetNode Node { get; } = node;
    public double X { get; internal set; } = x;
    public double Y { get; internal set; } = y;
}
