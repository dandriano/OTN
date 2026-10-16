using OTN.Core;

namespace OTN.Editor;

/// <summary>Canvas-only state associated with a network node.</summary>
public sealed class EditorNode(NetNode node, double x, double y)
{
    public NetNode Node { get; } = node;
    public double X { get; internal set; } = x;
    public double Y { get; internal set; } = y;
    public bool CtrlTagged { get; internal set; }
}
