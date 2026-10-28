using System.Collections.Generic;
using Godot;

namespace WarehouseRunner.Level;

// One rack of the warehouse background as its own canvas item, so racks outside
// the view are culled instead of the whole background being drawn every frame.
public partial class BackgroundRack : Node2D
{
    private readonly List<(Rect2 Rect, Color Color, bool Filled)> _rects = new();
    private readonly List<(Vector2 From, Vector2 To, Color Color)> _lines = new();
    private readonly List<(Vector2[] Points, Color Color)> _polygons = new();

    public void AddRect(Rect2 rect, Color color, bool filled = true) => _rects.Add((rect, color, filled));

    public void AddLine(Vector2 from, Vector2 to, Color color) => _lines.Add((from, to, color));

    public void AddPolygon(Vector2[] points, Color color) => _polygons.Add((points, color));

    public override void _Draw()
    {
        foreach (var (rect, color, filled) in _rects)
        {
            if (filled)
            {
                DrawRect(rect, color);
            }
            else
            {
                DrawRect(rect, color, false, 2f);
            }
        }

        foreach (var (from, to, color) in _lines)
        {
            DrawLine(from, to, color, 2f);
        }

        foreach (var (points, color) in _polygons)
        {
            DrawColoredPolygon(points, color);
        }
    }
}
