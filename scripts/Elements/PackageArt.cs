using Godot;

namespace WarehouseRunner.Elements;

public static class PackageArt
{
    private static readonly Color Gold = new(1f, 0.78f, 0.15f);
    private static readonly Color GoldDark = new(0.72f, 0.5f, 0.05f);
    private static readonly Color Tape = new(0.85f, 0.15f, 0.12f);

    public static void Draw(CanvasItem canvas, Vector2 center, float size, float glow = 0f)
    {
        if (glow > 0f)
        {
            canvas.DrawCircle(center, size * (0.95f + (glow * 0.25f)), new Color(1f, 0.85f, 0.3f, 0.18f + (glow * 0.12f)));
        }

        var rect = new Rect2(center - (Vector2.One * size / 2f), Vector2.One * size);
        canvas.DrawRect(rect, Gold);
        canvas.DrawRect(rect, GoldDark, false, Mathf.Max(2f, size * 0.07f));
        canvas.DrawRect(new Rect2(center.X - (size * 0.08f), rect.Position.Y, size * 0.16f, size), Tape);
        if (size >= 40f)
        {
            canvas.DrawString(ThemeDB.FallbackFont, center + new Vector2(-size * 0.45f, size * 0.38f), "EXPRESS", HorizontalAlignment.Left, -1, Mathf.RoundToInt(size * 0.2f), Colors.White);
        }
    }
}
