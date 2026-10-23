using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class BubbleWrap : BoxBody
{
    private const float PopDuration = 0.5f;
    private const float MinFallSpeed = 60f;

    private static readonly Color Base = new(0.35f, 0.38f, 0.42f);
    private static readonly Color Bubble = new(0.8f, 0.92f, 1f, 0.75f);
    private static readonly Color Shine = new(1f, 1f, 1f, 0.9f);

    private Area2D? _surface;
    private float _popTimer;
    private float _popX;

    public BubbleWrap()
    {
        Size = new Vector2(120f, 30f);
    }

    [Export]
    public float BounceSpeed { get; set; } = 1150f;

    public override void _Ready()
    {
        base._Ready();
        _surface = new Area2D { Position = new Vector2(0f, -Size.Y / 2f - 10f) };
        _surface.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(Size.X, 20f) } });
        AddChild(_surface);
    }

    public override void _PhysicsProcess(double delta)
    {
        _popTimer -= (float)delta;
        if (Engine.IsEditorHint() || _surface is null)
        {
            return;
        }

        foreach (var body in _surface.GetOverlappingBodies())
        {
            if (body is PlayerController player && player.Velocity.Y > MinFallSpeed)
            {
                player.Launch(new Vector2(player.Velocity.X, -BounceSpeed));
                _popTimer = PopDuration;
                _popX = ToLocal(player.GlobalPosition).X;
            }
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = LocalRect;
        DrawRect(rect, Base);
        var squash = _popTimer > 0f ? 1f - (_popTimer / PopDuration * 0.5f) : 1f;
        for (var x = rect.Position.X + 10f; x < rect.End.X - 4f; x += 18f)
        {
            var popped = _popTimer > 0f && Mathf.Abs(x - _popX) < 20f;
            var radius = popped ? 4f : 8f * squash;
            DrawCircle(new Vector2(x, rect.Position.Y - 4f), radius, Bubble);
            DrawCircle(new Vector2(x - 2f, rect.Position.Y - 6f), 2f, Shine);
        }

        if (_popTimer > 0f)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(_popX - 30f, rect.Position.Y - 30f - ((PopDuration - _popTimer) * 60f)), "PLOPP!", HorizontalAlignment.Left, -1, 20, new Color(1f, 1f, 1f, _popTimer / PopDuration));
        }
    }
}
