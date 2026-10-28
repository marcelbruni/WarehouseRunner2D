using Godot;

namespace WarehouseRunner.Elements;

public enum PlatformLook
{
    Crate,
    Drone,
    Robot,
    Forklift,
}

[Tool]
[GlobalClass]
public partial class MovingPlatform : AnimatableBody2D
{
    private static readonly Color Wood = new(0.69f, 0.48f, 0.24f);
    private static readonly Color WoodDark = new(0.43f, 0.29f, 0.13f);
    private static readonly Color Metal = new(0.25f, 0.27f, 0.3f);
    private static readonly Color MetalLight = new(0.55f, 0.58f, 0.62f);
    private static readonly Color RobotBody = new(0.2f, 0.55f, 0.85f);
    private static readonly Color Warning = new(1f, 0.82f, 0.1f);
    private static readonly Color PathHint = new(1f, 1f, 1f, 0.25f);
    private static readonly Color Tyre = new(0.12f, 0.12f, 0.13f);
    private static readonly Color CabGlass = new(0.55f, 0.75f, 0.9f, 0.5f);

    private Vector2 _size = new(120f, 30f);
    private RectangleShape2D? _shape;
    private Vector2 _origin;
    private float _time;

    [Export]
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            if (_shape is not null)
            {
                _shape.Size = value;
            }

            QueueRedraw();
        }
    }

    [Export]
    public Vector2 Travel { get; set; } = new(300f, 0f);

    [Export]
    public float Period { get; set; } = 3f;

    [Export(PropertyHint.Range, "0,1")]
    public float Phase { get; set; }

    [Export]
    public PlatformLook Look { get; set; } = PlatformLook.Crate;

    protected float AnimationTime => _time;

    public override void _Ready()
    {
        _shape = new RectangleShape2D { Size = _size };
        AddChild(new CollisionShape2D { Shape = _shape });
        _origin = Position;
        SyncToPhysics = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        if (Engine.IsEditorHint())
        {
            return;
        }

        var cycle = (_time / Period) + Phase;
        Position = _origin + (Travel * (0.5f - (0.5f * Mathf.Cos(cycle * Mathf.Tau))));
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(-_size / 2f, _size);
        if (Engine.IsEditorHint())
        {
            DrawLine(Vector2.Zero, Travel, PathHint, 2f);
        }

        switch (Look)
        {
            case PlatformLook.Drone:
                DrawDrone(rect);
                break;
            case PlatformLook.Robot:
                DrawRobot(rect);
                break;
            case PlatformLook.Forklift:
                DrawForklift(rect);
                break;
            default:
                DrawCrate(rect);
                break;
        }
    }

    private void DrawCrate(Rect2 rect)
    {
        DrawRect(rect, Wood);
        DrawRect(rect, WoodDark, false, 4f);
        DrawLine(rect.Position, rect.End, WoodDark, 3f);
        DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.End.X, rect.Position.Y), WoodDark, 3f);
        DrawArrow(new Vector2(0f, rect.End.Y + 10f), Travel.Normalized() * 14f);
    }

    private void DrawDrone(Rect2 rect)
    {
        DrawRect(rect, Metal);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 4f)), Warning);
        var spin = _time * 40f;
        foreach (var x in new[] { rect.Position.X + 14f, rect.End.X - 14f })
        {
            var hub = new Vector2(x, rect.End.Y + 8f);
            DrawLine(new Vector2(x, rect.End.Y), hub, MetalLight, 3f);
            var blade = new Vector2(Mathf.Cos(spin) * 22f, Mathf.Sin(spin) * 3f);
            DrawLine(hub - blade, hub + blade, MetalLight, 3f);
        }

        var blink = Mathf.PosMod(_time, 1f) < 0.5f ? new Color(1f, 0.2f, 0.2f) : new Color(0.3f, 0.05f, 0.05f);
        DrawCircle(new Vector2(0f, rect.End.Y - 4f), 4f, blink);
    }

    private void DrawRobot(Rect2 rect)
    {
        DrawRect(rect, RobotBody);
        DrawRect(new Rect2(rect.Position.X, rect.Position.Y, rect.Size.X, 6f), Warning);
        DrawCircle(new Vector2(rect.Position.X + 16f, rect.End.Y), 8f, Metal);
        DrawCircle(new Vector2(rect.End.X - 16f, rect.End.Y), 8f, Metal);
        var direction = Mathf.Sign(Mathf.Sin(((_time / Period) + Phase) * Mathf.Tau) * Travel.X);
        var eye = new Vector2(direction * rect.Size.X * 0.25f, rect.Position.Y + (rect.Size.Y * 0.45f));
        DrawCircle(eye, 7f, Colors.White);
        DrawCircle(eye + new Vector2(direction * 2f, 0f), 3.5f, Colors.Black);
        DrawLine(new Vector2(0f, rect.Position.Y), new Vector2(0f, rect.Position.Y - 12f), Metal, 2f);
        DrawCircle(new Vector2(0f, rect.Position.Y - 14f), 3f, Mathf.PosMod(_time * 3f, 1f) < 0.5f ? Warning : Metal);
    }

    // The truck stays where the platform started; only the carried crates ride the forks.
    private void DrawForklift(Rect2 rect)
    {
        var ground = _origin - Position + new Vector2(0f, rect.Size.Y / 2f);
        var mastX = rect.Position.X - 10f;
        var mastTop = ground.Y - rect.Size.Y - Mathf.Abs(Travel.Y) - 40f;
        var body = new Rect2(mastX - 150f, ground.Y - 64f, 140f, 50f);

        DrawRect(new Rect2(mastX - 6f, mastTop, 6f, ground.Y - mastTop), Metal);
        DrawRect(new Rect2(mastX + 2f, mastTop, 4f, ground.Y - mastTop), MetalLight);
        DrawRect(body, Warning);
        DrawRect(body, Metal, false, 3f);
        DrawRect(new Rect2(body.Position.X - 18f, body.Position.Y - 6f, 26f, 56f), Metal);
        var roof = body.Position.Y - 70f;
        DrawLine(new Vector2(body.Position.X + 20f, body.Position.Y), new Vector2(body.Position.X + 20f, roof), Metal, 5f);
        DrawLine(new Vector2(body.End.X - 14f, body.Position.Y), new Vector2(body.End.X - 14f, roof), Metal, 5f);
        DrawLine(new Vector2(body.Position.X + 12f, roof), new Vector2(body.End.X - 6f, roof), Metal, 7f);
        DrawRect(new Rect2(body.Position.X + 24f, roof + 6f, body.Size.X - 42f, body.Position.Y - roof - 8f), CabGlass);
        foreach (var x in new[] { body.Position.X + 28f, body.End.X - 22f })
        {
            DrawCircle(new Vector2(x, ground.Y - 14f), 14f, Tyre);
            DrawCircle(new Vector2(x, ground.Y - 14f), 5f, MetalLight);
        }

        DrawRect(new Rect2(mastX - 4f, rect.End.Y - 30f, 10f, 34f), Metal);
        DrawRect(new Rect2(mastX, rect.End.Y, rect.Size.X + 6f, 6f), Metal);

        var rows = Mathf.Max(1, Mathf.RoundToInt(rect.Size.Y / 96f));
        var cell = new Vector2(rect.Size.X, rect.Size.Y / rows);
        for (var row = 0; row < rows; row++)
        {
            var crate = new Rect2(rect.Position + new Vector2(0f, cell.Y * row), cell);
            DrawRect(crate, Wood);
            DrawRect(crate, WoodDark, false, 6f);
            var inner = crate.Grow(-6f);
            DrawLine(inner.Position, inner.End, WoodDark, 5f);
            DrawLine(new Vector2(inner.Position.X, inner.End.Y), new Vector2(inner.End.X, inner.Position.Y), WoodDark, 5f);
        }
    }

    private void DrawArrow(Vector2 at, Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            return;
        }

        DrawLine(at - direction, at + direction, PathHint, 2f);
        DrawLine(at - direction, at - direction + direction.Rotated(0.5f) * 0.5f, PathHint, 2f);
        DrawLine(at + direction, at + direction - direction.Rotated(0.5f) * 0.5f, PathHint, 2f);
    }
}
