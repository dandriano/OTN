

using OTN.Interfaces;
using Excubo.Blazor.Canvas.Contexts;
using OTN.Wasm.Extensions;
using System.Threading.Tasks;

namespace OTN.Wasm;

/// <summary>
/// A basic renderer for a bidirectional graph
/// </summary>
/// <remarks>
/// Make generic someday? And also generic <see cref="INetwork"/>.
/// </remarks>
public class NetworkRenderer
{
    private readonly NetworkSettings _settings;

    public NetworkRenderer(NetworkSettings settings)
    {
        _settings = settings;
    }
    
    public async ValueTask Render(Context2D ctx)   //, INetwork network)
    {
        await ctx.DrawGrid(_settings);
        // ctx.DrawVertices(network, _settings);
        // ctx.DrawEdges(network.Edges)
    }
}