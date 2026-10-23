using System.Collections.Generic;
using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class LaunchRamp : StaticBody2D
{
    private const int Segments = 12;
    private const float Thickness = 24f;
    private const float ChevronSpacing = 30f;

    private static readonly Color Rubber = new(0.16f, 0.17f, 0.19f);
    private static readonly Color Frame = new(0.55f, 0.57f, 0.6f);
    private static readonly Color Chevron = new(1f, 0.55f, 0.15f);

    private Area2D? _lip;
    private float _scroll;
    private float _flash;

    [Export]
    public float Radius { get; set; } = 220f;

    [Export(PropertyHint.Range, "10,44")]
    public float EndAngleDegrees { get; set; } = 40f;

    [Export]
    public Vector2 LaunchVelocity { get; set; } = new(380f, -1450f);

    private Vector2 Lip => PointAt(Mathf.DegToRad(EndAngleDegrees));

    public override void _Ready()
    {
        var surface = Surface();
        var polygon = new List<Vector2>(surface);
        polygon.Add(new Vector2(Lip.X, Thickness));
        polygon.Add(new Vector2(0f, Thickness));
        AddChild(new CollisionPolygon2D { Polygon = polygon.ToArray() });
        ConstantLinearVelocity = new Vector2(250f, 0f);

        _lip = new Area2D { Position = Lip + new Vector2(-10f, -30f) };
        _lip.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(30f, 60f) } });
        AddChild(_lip);
    }

    public override void _PhysicsProcess(double delta)
    {
        _flash = Mathf.Max(0f, _flash - (float)delta);
        if (Engine.IsEditorHint() || _lip is null)
        {
            return;
        }

        foreach (var body in _lip.GetOverlappingBodies())
        {
            if (body is PlayerController player && player.Velocity.X > 0f && player.State != PlayerState.Airborne)
            {
                player.Launch(LaunchVelocity);
                _flash = 0.4f;
            }
        }
    }

    public override void _Process(double delta)
    {
        _scroll = Mathf.PosMod(_scroll + ((float)delta * 400f), ChevronSpacing);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var surface = Surface();
        var outline = new List<Vector2>(surface) { new(Lip.X, Thickness), new(0f, Thickness) };
        DrawColoredPolygon(outline.ToArray(), Frame);
        DrawPolyline(surface, _flash > 0f ? Colors.White : Rubber, 10f);

        var length = Radius * Mathf.DegToRad(EndAngleDegrees);
        for (var d = _scroll; d < length; d += ChevronSpacing)
        {
            var angle = d / Radius;
            var at = PointAt(angle);
            var tangent = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
            var normal = new Vector2(-tangent.Y, tangent.X);
            DrawPolyline([at - (tangent * 5f) + (normal * 4f), at + (tangent * 4f), at - (tangent * 5f) - (normal * 4f)], Chevron, 2f);
        }

        DrawString(ThemeDB.FallbackFont, Lip + new Vector2(-40f, -50f), "HUII!", HorizontalAlignment.Left, -1, 16, Chevron with { A = _flash > 0f ? 1f : 0.35f });
    }

    private Vector2[] Surface()
    {
        var points = new Vector2[Segments + 1];
        var end = Mathf.DegToRad(EndAngleDegrees);
        for (var i = 0; i <= Segments; i++)
        {
            points[i] = PointAt(end * i / Segments);
        }

        return points;
    }

    private Vector2 PointAt(float angle) => new(Radius * Mathf.Sin(angle), -Radius * (1f - Mathf.Cos(angle)));
}
