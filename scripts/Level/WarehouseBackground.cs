using System.Collections.Generic;
using Godot;

namespace WarehouseRunner.Level;

[Tool]
[GlobalClass]
public partial class WarehouseBackground : Node2D
{
    private const float RackSpacing = 420f;
    private const float RackWidth = 300f;
    private const float ShelfSpacing = 110f;

    private static readonly Color Wall = new(0.2f, 0.22f, 0.26f);
    private static readonly Color WallStripe = new(0.24f, 0.26f, 0.3f);
    private static readonly Color RackUpright = new(0.18f, 0.32f, 0.55f);
    private static readonly Color RackBeam = new(0.85f, 0.45f, 0.12f);
    private static readonly Color Cardboard = new(0.5f, 0.38f, 0.25f);
    private static readonly Color CardboardDark = new(0.4f, 0.3f, 0.2f);
    private static readonly Color LampLight = new(1f, 0.95f, 0.75f, 0.05f);

    [Export]
    public float Width { get; set; } = 5000f;

    [Export]
    public float Height { get; set; } = 1400f;

    [Export]
    public float Depth { get; set; } = 400f;

    private const float CoverMargin = 200f;

    private readonly List<BackgroundRack> _racks = new();
    private float _left = -1000f;
    private float _authoredWidth;
    private float _authoredHeight;
    private float _authoredDepth;

    public override void _Ready()
    {
        _authoredWidth = Width;
        _authoredHeight = Height;
        _authoredDepth = Depth;
        if (Engine.IsEditorHint())
        {
            RebuildRacks();
            return;
        }

        FitToCameraBounds();
        GetViewport().SizeChanged += FitToCameraBounds;
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
        {
            GetViewport().SizeChanged -= FitToCameraBounds;
        }
    }

    // Grows the drawn area so every camera position allowed by the level's
    // CameraBounds sees background, taking the parallax scroll into account.
    private void FitToCameraBounds()
    {
        var level = FindLevel();
        if (level == null)
        {
            return;
        }

        var scale = Vector2.One;
        var offset = Vector2.Zero;
        if (GetParent() is Parallax2D parallax)
        {
            scale = parallax.ScrollScale;
            offset = parallax.ScrollOffset;
        }

        var bounds = level.CameraBounds;
        var view = GetViewportRect().Size;
        var screenMin = (Vector2)bounds.Position;
        var screenMax = new Vector2(
            Mathf.Max(bounds.Position.X, bounds.End.X - view.X),
            Mathf.Max(bounds.Position.Y, bounds.End.Y - view.Y));

        // Parallax2D shifts its children by screenOffset * (1 - scale) + offset,
        // so a screen whose top-left is at S shows local coordinates S * scale - offset.
        var coverStart = (screenMin * scale) - offset - new Vector2(CoverMargin, CoverMargin);
        var coverEnd = (screenMax * scale) - offset + view + new Vector2(CoverMargin, CoverMargin);

        _left = Mathf.Min(-1000f, coverStart.X);
        Width = Mathf.Max(_authoredWidth, coverEnd.X);
        Height = Mathf.Max(_authoredHeight, -coverStart.Y);
        Depth = Mathf.Max(_authoredDepth, coverEnd.Y);
        QueueRedraw();
        RebuildRacks();
    }

    private RaceController? FindLevel()
    {
        for (var node = GetParent(); node != null; node = node.GetParent())
        {
            if (node is RaceController level)
            {
                return level;
            }
        }

        return null;
    }

    public override void _Draw()
    {
        var wallWidth = Width + 1000f - _left;
        DrawRect(new Rect2(_left, -Height, wallWidth, Height + Depth), Wall);
        for (var y = -Height; y < Depth; y += 160f)
        {
            DrawRect(new Rect2(_left, y, wallWidth, 6f), WallStripe);
        }

    }

    private void RebuildRacks()
    {
        foreach (var rack in _racks)
        {
            rack.QueueFree();
        }

        _racks.Clear();
        var seed = 7u;
        for (var x = -600f; x < Width; x += RackSpacing)
        {
            var rack = new BackgroundRack();
            AddRack(rack, x, ref seed);
            AddLamp(rack, x + (RackSpacing / 2f));
            AddChild(rack);
            _racks.Add(rack);
        }
    }

    private void AddRack(BackgroundRack rack, float left, ref uint seed)
    {
        var top = -Height + 300f;
        rack.AddRect(new Rect2(left, top, 8f, -top), RackUpright);
        rack.AddRect(new Rect2(left + RackWidth - 8f, top, 8f, -top), RackUpright);

        for (var y = -40f; y > top; y -= ShelfSpacing)
        {
            rack.AddRect(new Rect2(left, y, RackWidth, 6f), RackBeam);
            var x = left + 12f;
            while (x < left + RackWidth - 50f)
            {
                var boxWidth = 30f + (NextRandom(ref seed) % 40);
                var boxHeight = 30f + (NextRandom(ref seed) % 50);
                if (NextRandom(ref seed) % 4 != 0)
                {
                    var box = new Rect2(x, y - boxHeight, boxWidth, boxHeight);
                    rack.AddRect(box, Cardboard);
                    rack.AddRect(box, CardboardDark, filled: false);
                }

                x += boxWidth + 6f;
            }
        }
    }

    private void AddLamp(BackgroundRack rack, float x)
    {
        var lampY = -Height + 120f;
        rack.AddLine(new Vector2(x, -Height), new Vector2(x, lampY), Colors.Black);
        rack.AddRect(new Rect2(x - 30f, lampY, 60f, 10f), new Color(0.9f, 0.9f, 0.85f));
        rack.AddPolygon([new Vector2(x - 30f, lampY + 10f), new Vector2(x + 30f, lampY + 10f), new Vector2(x + 220f, 0f), new Vector2(x - 220f, 0f)], LampLight);
    }

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}
