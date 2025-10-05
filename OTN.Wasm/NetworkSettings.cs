namespace OTN.Wasm;

/// <summary>
/// A state of visual settings for a graph: sizes, colors, paths, etc
/// </summary>
public class NetworkSettings
{
    public static NetworkSettings Default => new NetworkSettings();
    public int Width { get; set; }
    public int Height { get; set; }
    public string BgColour { get; private set; } = string.Empty;
    public string FgColour { get; private set; } = string.Empty;
    public string VertexBgColour { get; private set; } = string.Empty;
    public string VertexFgColour { get; private set; } = string.Empty;
    public int Spacing { get; private set; } = 10;

    public void SetSize(int width, int height, int spacing = 10) 
    {
        Width = width;
        Height = height;
        Spacing = spacing;
    }

    public void SetColour(string fg, string bg) 
    {
        FgColour = fg;
        BgColour = bg;
    }

    public void SetVertexColour(string fg, string bg) 
    {
        VertexFgColour = fg;
        VertexBgColour = bg;
    }
}