using Godot;

namespace WarehouseRunner.Core;

public partial class MedalBadge : Control
{
    private static readonly Color RibbonLeft = new(0.78f, 0.13f, 0.16f);
    private static readonly Color RibbonRight = new(0.58f, 0.08f, 0.12f);
    private static readonly Color Locked = new(0.4f, 0.42f, 0.46f);
    private static readonly Color Shine = new(1f, 1f, 1f, 0.45f);

    public Medal Medal { get; private set; }

    public bool Earned { get; private set; }

    public static MedalBadge Create(Medal medal, bool earned, float width) => new()
    {
        Medal = medal,
        Earned = earned,
        CustomMinimumSize = new Vector2(width, width * 1.25f),
        MouseFilter = MouseFilterEnum.Ignore,
    };

    public override void _Draw()
    {
        var w = Size.X;
        var h = Size.Y;
        var baseColor = Earned ? MedalTimes.ColorOf(Medal) : Locked;
        var alpha = Earned ? 1f : 0.45f;
        Color Faded(Color color) => new(color, color.A * alpha);

        var ribbonLeft = Earned ? RibbonLeft : Locked.Darkened(0.25f);
        var ribbonRight = Earned ? RibbonRight : Locked.Darkened(0.4f);
        DrawColoredPolygon([new(w * 0.2f, 0f), new(w * 0.42f, 0f), new(w * 0.58f, h * 0.5f), new(w * 0.36f, h * 0.5f)], Faded(ribbonLeft));
        DrawColoredPolygon([new(w * 0.58f, 0f), new(w * 0.8f, 0f), new(w * 0.64f, h * 0.5f), new(w * 0.42f, h * 0.5f)], Faded(ribbonRight));

        var center = new Vector2(w * 0.5f, h - (w * 0.42f));
        var radius = w * 0.4f;
        DrawCircle(center, radius, Faded(baseColor.Darkened(0.35f)));
        DrawCircle(center, radius * 0.84f, Faded(baseColor));
        DrawArc(center, radius * 0.7f, 0f, Mathf.Tau, 32, Faded(baseColor.Darkened(0.2f)), Mathf.Max(1f, w * 0.03f), true);
        DrawColoredPolygon(Star(center, radius * 0.5f), Faded(Earned ? baseColor.Lightened(0.35f) : baseColor.Darkened(0.15f)));
        if (Earned)
        {
            DrawArc(center, radius * 0.66f, Mathf.Pi * 1.05f, Mathf.Pi * 1.45f, 12, Shine, Mathf.Max(1.5f, w * 0.06f), true);
        }
    }

    private static Vector2[] Star(Vector2 center, float radius)
    {
        var points = new Vector2[10];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = (-Mathf.Pi / 2f) + (i * Mathf.Pi / 5f);
            var r = i % 2 == 0 ? radius : radius * 0.45f;
            points[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
        }

        return points;
    }
}
