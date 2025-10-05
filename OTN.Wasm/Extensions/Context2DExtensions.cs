

using OTN.Interfaces;
using Excubo.Blazor.Canvas.Contexts;
using System;
using System.Threading.Tasks;


namespace OTN.Wasm.Extensions;
public static class IContext2DExtensions
{
    /// <summary>
    /// Clear entire frame.
    /// </summary>
    /// <param name="ctx">The rendering context.</param>
    /// <param name="settings"></param>
    public static ValueTask ClearFrame(this Context2D ctx, NetworkSettings settings)
        => ctx.ClearRectAsync(0, 0, settings.Width, settings.Height);
    /// <summary>
    /// Fill entire frame.
    /// </summary>
    /// <param name="ctx">The rendering context.</param>
    /// <param name="settings"></param>
    public static async ValueTask FillFrame(this Context2D ctx, NetworkSettings settings)
    {
        await ctx.FillAndStrokeStyles.FillStyleAsync(settings.BgColour);
        await ctx.FillRectAsync(0, 0, settings.Width, settings.Height);
    }
    /// <summary>
    /// Draws a line on the rendering context from a starting point to an ending point.
    /// </summary>
    /// <param name="ctx">The rendering context to draw on.</param>
    /// <param name="xs">The X coordinate of the starting point of the line.</param>
    /// <param name="ys">The Y coordinate of the starting point of the line.</param>
    /// <param name="xe">The X coordinate of the ending point of the line.</param>
    /// <param name="ye">The Y coordinate of the ending point of the line.</param>
    /// <param name="colour">The color of the line (default is black).</param>
    /// <param name="width">The width (thickness) of the line (default is 1).</param>
    public static async ValueTask StrokeLine(this Context2D ctx, float xs, float ys, float xe, float ye, string colour = "black", int width = 1)
    {
        await ctx.BeginPathAsync();
        await ctx.MoveToAsync(xs, ys);
        await ctx.LineToAsync(xe, ye);

        await ctx.SetStroke(colour, width);
        await ctx.StrokeAsync();
    }
    /// <summary>
    /// Draw "cartesian" grid on canvas based on network settings
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="settings"></param>
    public static async Task DrawGrid(this Context2D ctx, NetworkSettings settings)
    {
        var centerX = settings.Width / 2;
        var centerY = settings.Height / 2;

        // redraw background
        await ctx.FillFrame(settings);

        // draw vertical
        for (var x = 0; x <= settings.Width; x += settings.Spacing)
        {
            // main Y axis
            if (x == centerX)
                await ctx.StrokeLine(x, 0, x, settings.Height, width: 3);
            // every 10th
            else if ((x - centerX) / settings.Spacing % 10 == 0)
                await ctx.StrokeLine(x, 0, x, settings.Height, width: 2);
            else
                await ctx.StrokeLine(x, 0, x, settings.Height, settings.FgColour);
        }

        // draw horizontal
        for (var y = 0; y <= settings.Height; y += settings.Spacing)
        {
            // main X axis
            if (y == centerY)
                await ctx.StrokeLine(0, y, settings.Width, y, width: 3);
            // every 10th
            else if ((y - centerY) / settings.Spacing % 10 == 0)
                await ctx.StrokeLine(0, y, settings.Width, y, width: 2);
            else
                await ctx.StrokeLine(0, y, settings.Width, y, settings.FgColour);
        }
    }
    /// <summary>
    /// Strokes an ellipse on the given rendering context by approximating it with line segments.
    /// </summary>
    /// <param name="ctx">The rendering context to draw on.</param>
    /// <param name="cx">The X coordinate of the ellipse center.</param>
    /// <param name="cy">The Y coordinate of the ellipse center.</param>
    /// <param name="rx">The radius of the ellipse along the X axis.</param>
    /// <param name="ry">The radius of the ellipse along the Y axis.</param>
    /// <param name="colour">The colour of the stroke.</param>
    /// <param name="segments">The number of line segments used to approximate the ellipse. Higher means smoother.</param>
    public static async Task StrokeEllipse(this Context2D ctx, float cx, float cy, float rx, float ry, string colour = "black", int segments = 100)
    {
        await ctx.BeginPathAsync();

        // loop through 'segments' points around the ellipse perimeter
        for (var i = 0; i <= segments; i++)
        {
            // calculate angle for the current segment and determine coords
            var theta = (double)i / segments * 2.0 * Math.PI;
            var x = cx + rx * (float)Math.Cos(theta);
            var y = cy + ry * (float)Math.Sin(theta);

            if (i == 0)
                await ctx.MoveToAsync(x, y);
            else
                await ctx.LineToAsync(x, y);
        }

        await ctx.SetStroke(colour);
        await ctx.StrokeAsync();
    }
    /// <summary>
    /// Fills an ellipse on the given rendering context.
    /// </summary>
    /// <param name="ctx">The rendering context to draw on.</param>
    /// <param name="cx">The X coordinate of the ellipse center.</param>
    /// <param name="cy">The Y coordinate of the ellipse center.</param>
    /// <param name="rx">The radius of the ellipse along the X axis.</param>
    /// <param name="ry">The radius of the ellipse along the Y axis.</param>
    /// <param name="colour">The colour of the stroke.</param>
    public static async Task FillEllipse(this Context2D ctx, float cx, float cy, float rx, float ry, string colour = "black")
    {
        // bounding rectangle
        var left = cx - rx;
        var top = cy - ry;
        var width = 2 * rx;
        var height = 2 * ry;

        // iterate through each point within the bounding rectangle
        for (var x = left; x < left + width; x++)
        {
            for (var y = top; y < top + height; y++)
            {
                // check if the point (x, y) is inside the ellipse
                var normalizedX = (x - cx) / rx;
                var normalizedY = (y - cy) / ry;

                if ((normalizedX * normalizedX + normalizedY * normalizedY) <= 1)
                {
                    // fill a 1x1 rectangle at (x, y) if inside the ellipse
                    await ctx.FillAndStrokeStyles.FillStyleAsync(colour);
                    await ctx.FillRectAsync(x, y, 1, 1);
                }
            }
        }
    }
    /// <summary>
    /// Shortcut for setting stroke style
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="colour"></param>
    /// <param name="width"></param>
    public static async ValueTask SetStroke(this Context2D ctx, string colour = "black", int width = 1)
    {
        await ctx.FillAndStrokeStyles.StrokeStyleAsync(colour);
        await ctx.LineWidthAsync(width);
    }
    /// <summary>
    /// Draw network verticies (if needed)
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="network"></param>
    public static void DrawVertices(this Context2D ctx, INetwork network, NetworkSettings settings)
    {
        /*
        foreach (var v in network.Vertices)
        {
            if (!network.VerticesMap.TryGetValue(v.Id, out var coords))
                continue;

            // network.VerticesMap.Remove(v.Id);

            ctx.FillEllipse(coords.x, coords.y, 5, 5, settings.VertexBgColour);
            ctx.StrokeEllipse(coords.x, coords.y, 5, 5, settings.VertexFgColour);
        }
        */
    }
}
