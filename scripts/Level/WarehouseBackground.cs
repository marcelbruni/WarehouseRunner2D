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

    public override void _Draw()
    {
        DrawRect(new Rect2(-1000f, -Height, Width + 2000f, Height + 400f), Wall);
        for (var y = -Height; y < 0f; y += 160f)
        {
            DrawRect(new Rect2(-1000f, y, Width + 2000f, 6f), WallStripe);
        }

        var seed = 7u;
        for (var x = -600f; x < Width; x += RackSpacing)
        {
            DrawRack(x, ref seed);
            DrawLamp(x + (RackSpacing / 2f));
        }
    }

    private void DrawRack(float left, ref uint seed)
    {
        var top = -Height + 300f;
        DrawRect(new Rect2(left, top, 8f, -top), RackUpright);
        DrawRect(new Rect2(left + RackWidth - 8f, top, 8f, -top), RackUpright);

        for (var y = -40f; y > top; y -= ShelfSpacing)
        {
            DrawRect(new Rect2(left, y, RackWidth, 6f), RackBeam);
            var x = left + 12f;
            while (x < left + RackWidth - 50f)
            {
                var boxWidth = 30f + (NextRandom(ref seed) % 40);
                var boxHeight = 30f + (NextRandom(ref seed) % 50);
                if (NextRandom(ref seed) % 4 != 0)
                {
                    var box = new Rect2(x, y - boxHeight, boxWidth, boxHeight);
                    DrawRect(box, Cardboard);
                    DrawRect(box, CardboardDark, false, 2f);
                }

                x += boxWidth + 6f;
            }
        }
    }

    private void DrawLamp(float x)
    {
        var lampY = -Height + 120f;
        DrawLine(new Vector2(x, -Height), new Vector2(x, lampY), Colors.Black, 2f);
        DrawRect(new Rect2(x - 30f, lampY, 60f, 10f), new Color(0.9f, 0.9f, 0.85f));
        DrawColoredPolygon([new Vector2(x - 30f, lampY + 10f), new Vector2(x + 30f, lampY + 10f), new Vector2(x + 220f, 0f), new Vector2(x - 220f, 0f)], LampLight);
    }

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}
