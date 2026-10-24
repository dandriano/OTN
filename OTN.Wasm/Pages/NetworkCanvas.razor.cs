using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorex;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OTN.Core;
using OTN.Enums;
using OTN.Wasm.ViewModels;

namespace OTN.Wasm.Pages;

public partial class NetworkCanvas : ComponentBase, IDisposable
{
    private enum EditorMode { Select, Edit }
    private enum CreationMode { Node, Link }

    [Parameter, EditorRequired] public NetworkEditorViewModel Document { get; set; } = null!;
    [Parameter] public double Zoom { get; set; } = 1;
    [Parameter] public EventCallback<double> ZoomChanged { get; set; }
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private CanvasBase? _canvas;
    private bool _isDirty = true;
    private double _lastZoom = double.NaN;
    private EditorMode _mode = EditorMode.Select;
    private CreationMode _creationMode = CreationMode.Node;
    private NetNodeType _newNodeType = NetNodeType.Terminal;
    private LinkType _newLinkType = LinkType.Duplex;
    private double _panX, _panY, _pointerX, _pointerY, _dragStartX, _dragStartY, _panStartX, _panStartY, _panOriginX, _panOriginY;
    private NodeViewModel? _dragNode;
    private NodeViewModel? _hoveredNode;
    private bool _panning;
    private HashSet<Link> _highlightedLinks = [];

    protected override void OnInitialized() => Document.PropertyChanged += DocumentChanged;

    private void DocumentChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _isDirty = true;
        if (e.PropertyName is nameof(NetworkEditorViewModel.Revision) or nameof(NetworkEditorViewModel.Routes) or nameof(NetworkEditorViewModel.SelectedRouteIndex))
            _highlightedLinks = Document.SelectedRouteIndex >= 0 ? Document.Routes[Document.SelectedRouteIndex].Links.ToHashSet() : [];
        _ = InvokeAsync(StateHasChanged);
    }

    protected override void OnParametersSet()
    {
        if (_lastZoom != Zoom)
        {
            _lastZoom = Zoom;
            _isDirty = true;
        }
    }

    public void Dispose() => Document.PropertyChanged -= DocumentChanged;

    public void ResetView()
    {
        _panX = 0;
        _panY = 0;
        _isDirty = true;
    }

    private NodeViewModel NodeFor(NetNode node) => Document.Nodes.First(item => ReferenceEquals(item.Node, node));
    private bool IsHighlighted(Link link) => _highlightedLinks.Contains(link);

    private void SetNewNodeType(NetNodeType type) => _newNodeType = type;
    private void SetNewLinkType(LinkType type) => _newLinkType = type;

    private void SetMode(EditorMode mode)
    {
        _mode = mode;
        if (mode != EditorMode.Edit || _creationMode != CreationMode.Link)
            Document.CancelPendingLinkCommand.Execute(null);
    }

    private void SetCreationMode(CreationMode mode)
    {
        _creationMode = mode;
        if (mode != CreationMode.Link) Document.CancelPendingLinkCommand.Execute(null);
    }

    private void SelectLink(LinkViewModel link) => Document.SelectLink(link);

    private void CanvasReady(CanvasBase canvas)
    {
        _canvas = canvas;
        _isDirty = true;
    }

    private void DrawFrame(float timestamp)
    {
        if (_canvas is null || !_isDirty)
            return;

        var context = _canvas.RenderContext;
        context.ClearRect(0, 0, 1000, 680);
        context.FillStyle = "#f8fafc";
        context.FillRect(0, 0, 1000, 680);
        context.FillStyle = "#d9e1ea";
        for (var x = 12; x < 1000; x += 24)
        {
            for (var y = 12; y < 680; y += 24)
            {
                context.BeginPath(); 
                context.Arc(x, y, 1, 0, MathF.PI * 2, false); 
                context.Fill();
            }
        }
        context.Save();
        context.Translate((float)_panX, (float)_panY);
        context.Scale((float)Zoom, (float)Zoom);
        foreach (var item in Document.Links) 
            DrawLink(context, item.Link);
        if (Document.PendingLinkSource is { } pending)
        {
            context.StrokeStyle = "#3182e6"; context.LineWidth = 2;
            context.BeginPath(); 
            context.MoveTo((float)pending.X, (float)pending.Y); 
            context.LineTo((float)_pointerX, (float)_pointerY); 
            context.Stroke();
        }
        foreach (var item in Document.Nodes) 
            DrawNode(context, item);
        if (Document.Nodes.Count == 0)
        {
            context.TextAlign = TextAlign.Center;
            context.FillStyle = "#52667d"; context.Font = "700 18px sans-serif"; context.FillText("Start here", 500, 320);
            context.FillStyle = "#92a0ae"; context.Font = "12px sans-serif"; context.FillText("Choose Edit → Node, then click anywhere on the canvas", 500, 348);
        }
        context.Restore();
        _isDirty = false;
    }

    private void DrawLink(IRenderContext context, Link link)
    {
        var source = NodeFor(link.Source); var target = NodeFor(link.Target);
        var highlighted = IsHighlighted(link);
        var stroke = highlighted ? "#2679e8" : "#9aa9b8";
        var lineWidth = highlighted ? 4 : 2;
        var dx = target.X - source.X;
        var dy = target.Y - source.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance > 0)
        {
            var directionX = dx / distance;
            var directionY = dy / distance;
            var sourceClearance = NodeClearance(directionX, directionY);
            var targetClearance = NodeClearance(directionX, directionY);
            var startX = source.X + directionX * sourceClearance;
            var startY = source.Y + directionY * sourceClearance;
            var endX = target.X - directionX * targetClearance;
            var endY = target.Y - directionY * targetClearance;
            var visibleLength = distance - sourceClearance - targetClearance;
            if (visibleLength > 0)
            {
                context.StrokeStyle = stroke; context.LineWidth = lineWidth;
                context.BeginPath(); context.MoveTo((float)startX, (float)startY); context.LineTo((float)endX, (float)endY); context.Stroke();
                if (visibleLength >= 20)
                {
                    DrawArrow(context, endX, endY, dx, dy, stroke, lineWidth);
                    if (link.LinkType == LinkType.Duplex)
                        DrawArrow(context, startX, startY, -dx, -dy, stroke, lineWidth);
                }
            }
        }
        context.FillStyle = "#617184"; context.Font = "11px sans-serif"; context.TextAlign = TextAlign.Center;
        context.FillText(string.Concat(link.Tag.ToString("0.##"), " km"), (float)((source.X + target.X) / 2), (float)((source.Y + target.Y) / 2 - 8));
    }

    private static void DrawArrow(IRenderContext context, double tipX, double tipY, double directionX, double directionY, string stroke, int lineWidth)
    {
        var angle = Math.Atan2(directionY, directionX);
        const double length = 9, spread = .55;
        context.StrokeStyle = stroke; context.LineWidth = lineWidth; context.BeginPath();
        context.MoveTo((float)tipX, (float)tipY); context.LineTo((float)(tipX - length * Math.Cos(angle - spread)), (float)(tipY - length * Math.Sin(angle - spread)));
        context.MoveTo((float)tipX, (float)tipY); context.LineTo((float)(tipX - length * Math.Cos(angle + spread)), (float)(tipY - length * Math.Sin(angle + spread)));
        context.Stroke();
    }

    private void DrawNode(IRenderContext context, NodeViewModel item)
    {
        var required = Document.IsMustNode(item);
        var avoided = Document.IsAvoidNode(item);
        var fill = required ? "#238653" : avoided ? "#c94f4f" : _hoveredNode == item ? "#FFD700" : "#607D8B";

        context.Save();
        context.Translate((float)(item.X - 17), (float)(item.Y - 17));
        DrawNodeBadge(context, fill);
        if (item.Node.Type == NetNodeType.OLA)
            DrawOlaArtwork(context);
        else
            DrawTerminalArtwork(context);
        context.Restore();

        if (required || avoided)
        {
            context.FillStyle = "#FFFFFF";
            context.Font = "700 10px sans-serif";
            context.TextAlign = TextAlign.Center;
            context.FillText(required ? "R" : "A", (float)item.X + 11, (float)item.Y + 14);
        }

        context.FillStyle = "#314156";
        context.Font = "700 10px sans-serif";
        context.TextAlign = TextAlign.Center;
        context.FillText(item.Id, (float)item.X, (float)item.Y - 23);
    }

    private static void DrawNodeBadge(IRenderContext context, string fill)
    {
        context.FillStyle = fill;
        context.StrokeStyle = fill;
        context.LineWidth = 1;
        context.BeginPath();
        context.MoveTo(3.5f, 0.5f);
        context.LineTo(30.5f, 0.5f);
        context.LineTo(33.5f, 3.5f);
        context.LineTo(33.5f, 30.5f);
        context.LineTo(30.5f, 33.5f);
        context.LineTo(3.5f, 33.5f);
        context.LineTo(0.5f, 30.5f);
        context.LineTo(0.5f, 3.5f);
        context.ClosePath();
        context.Fill();
        context.Stroke();
    }

    private static void DrawTerminalArtwork(IRenderContext context)
    {
        context.FillStyle = "#FFFFFF";

        DrawPolygon(context, [(8.3f, 4.8f), (4.3f, 8.8f), (4f, 9.55f), (4f, 24.45f),
            (4.3f, 25.16f), (8.3f, 29.16f), (10f, 28.45f), (10f, 5.55f)]);
        DrawPolygon(context, [(25.7f, 4.8f), (29.7f, 8.8f), (30f, 9.55f), (30f, 24.45f),
            (29.7f, 25.16f), (25.7f, 29.16f), (24f, 28.45f), (24f, 5.55f)]);

        context.Font = "700 18px sans-serif";
        context.TextAlign = TextAlign.Center;
        context.FillText("λ", 17f, 23f);
    }

    private static void DrawOlaArtwork(IRenderContext context)
    {
        context.FillStyle = "#FFFFFF";
        DrawPolygon(context, [(11.4f, 8.6f), (23.9f, 15.9f), (24.7f, 16.8f),
            (23.8f, 18f), (11.4f, 25.2f), (9.5f, 24.1f), (9.5f, 9.7f)]);
    }

    private static void DrawPolygon(IRenderContext context, ReadOnlySpan<(float X, float Y)> points)
    {
        context.BeginPath();
        context.MoveTo(points[0].X, points[0].Y);
        for (var i = 1; i < points.Length; i++)
            context.LineTo(points[i].X, points[i].Y);
        context.ClosePath();
        context.Fill();
    }

    private static double NodeClearance(double directionX, double directionY)
    {
        const double halfSize = 17;
        return halfSize / Math.Max(Math.Abs(directionX), Math.Abs(directionY)) + 1;
    }

    private async Task<double[]> Point(PointerEventArgs e)
    {
        if (_canvas is null) 
            return [e.OffsetX, e.OffsetY];
        try 
        { 
            return await JS.InvokeAsync<double[]>("otnEditor.point", _canvas.ElementReference, e.ClientX, e.ClientY); 
        }
        catch 
        { 
            return [e.OffsetX, e.OffsetY]; 
        }
    }

    private async Task CanvasPointerDown(PointerEventArgs e)
    {
        var p = await Point(e); _pointerX = (p[0] - _panX) / Zoom; _pointerY = (p[1] - _panY) / Zoom;
        _isDirty = true;
        if (e.Button != 0)
        {
            _panning = true; _panStartX = p[0]; _panStartY = p[1]; _panOriginX = _panX; _panOriginY = _panY;
            return;
        }
        if (_mode == EditorMode.Edit && _creationMode == CreationMode.Node)
        {
            _hoveredNode = Document.AddNode(_newNodeType, _pointerX, _pointerY);
            return;
        }
        var hitNode = FindNodeAtPointer();
        _hoveredNode = hitNode;
        if (hitNode is not null)
        {
            if (_mode == EditorMode.Edit && _creationMode == CreationMode.Link)
            {
                if (Document.PendingLinkSource is null) Document.BeginLink(hitNode);
                else Document.CompleteLink(hitNode, 100, _newLinkType);
                return;
            }
            if (_mode == EditorMode.Select)
            {
                if (e.ShiftKey) { Document.ToggleMustNode(hitNode); return; }
                if (e.AltKey) { Document.ToggleAvoidNode(hitNode); return; }
                Document.SelectNode(hitNode); _dragNode = hitNode;
                _dragStartX = _pointerX - hitNode.X; _dragStartY = _pointerY - hitNode.Y;
            }
        }
        else if (_mode == EditorMode.Edit && _creationMode == CreationMode.Link) Document.CancelPendingLink();
        else if (_mode == EditorMode.Select)
        {
            var hitLink = Document.Links.LastOrDefault(item => DistanceToLink(_pointerX, _pointerY, item.Link) < 9);
            if (hitLink is not null) SelectLink(hitLink); else Document.SelectNode(null);
            _panning = true; _panStartX = p[0]; _panStartY = p[1]; _panOriginX = _panX; _panOriginY = _panY;
        }
    }

    private async Task CanvasPointerMove(PointerEventArgs e)
    {
        var p = await Point(e);
        var previousPointerX = _pointerX;
        var previousPointerY = _pointerY;
        _pointerX = (p[0] - _panX) / Zoom; _pointerY = (p[1] - _panY) / Zoom;
        if (_dragNode is { } node)
        {
            var nodeX = _pointerX - _dragStartX;
            var nodeY = _pointerY - _dragStartY;
            if (node.X != nodeX || node.Y != nodeY)
                Document.MoveNode(node, nodeX, nodeY);
        }
        if (_panning)
        {
            var panX = _panOriginX + p[0] - _panStartX;
            var panY = _panOriginY + p[1] - _panStartY;
            if (_panX != panX || _panY != panY)
            {
                _panX = panX;
                _panY = panY;
                _isDirty = true;
            }
        }
        _pointerX = (p[0] - _panX) / Zoom; _pointerY = (p[1] - _panY) / Zoom;
        var hoveredNode = FindNodeAtPointer();
        if (_hoveredNode != hoveredNode)
        {
            _hoveredNode = hoveredNode;
            _isDirty = true;
        }
        if (Document.PendingLinkSource is not null &&
            (previousPointerX != _pointerX || previousPointerY != _pointerY))
            _isDirty = true;
    }

    private void CanvasPointerUp(PointerEventArgs e) { _dragNode = null; _panning = false; }
    private void CanvasPointerLeave(PointerEventArgs e)
    {
        CanvasPointerUp(e);
        if (_hoveredNode is not null)
        {
            _hoveredNode = null;
            _isDirty = true;
        }
    }

    private NodeViewModel? FindNodeAtPointer() => Document.Nodes.LastOrDefault(item =>
        Math.Abs(item.X - _pointerX) <= 18 && Math.Abs(item.Y - _pointerY) <= 18);

    private double DistanceToLink(double x, double y, Link link)
    {
        var source = NodeFor(link.Source); var target = NodeFor(link.Target);
        var dx = target.X - source.X; var dy = target.Y - source.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return double.MaxValue;
        var t = Math.Clamp(((x - source.X) * dx + (y - source.Y) * dy) / lengthSquared, 0, 1);
        return Math.Sqrt(Math.Pow(x - (source.X + t * dx), 2) + Math.Pow(y - (source.Y + t * dy), 2));
    }

    private void CanvasKeyDown(KeyboardEventArgs e) { if (e.Key == "Escape") Document.CancelPendingLinkCommand.Execute(null); }
    private Task Wheel(WheelEventArgs e) => ZoomChanged.InvokeAsync(Math.Clamp(Zoom * (e.DeltaY < 0 ? 1.1 : 0.9), 0.35, 2.8));
}
