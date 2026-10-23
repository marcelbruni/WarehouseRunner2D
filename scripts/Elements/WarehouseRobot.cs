using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class WarehouseRobot : MovingPlatform
{
    private const float FeetTolerance = 6f;
    private const float BeepDuration = 0.6f;

    private Area2D? _bumper;
    private float _beepTimer;

    public WarehouseRobot()
    {
        Look = PlatformLook.Robot;
        Size = new Vector2(90f, 44f);
    }

    [Export]
    public Vector2 BumpVelocity { get; set; } = new(520f, -420f);

    public override void _Ready()
    {
        base._Ready();
        _bumper = new Area2D();
        _bumper.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = Size + new Vector2(10f, -8f) } });
        AddChild(_bumper);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _beepTimer -= (float)delta;
        if (Engine.IsEditorHint() || _bumper is null)
        {
            return;
        }

        var top = GlobalPosition.Y - (Size.Y / 2f);
        foreach (var body in _bumper.GetOverlappingBodies())
        {
            if (body is PlayerController player && player.GlobalPosition.Y + 26f > top + FeetTolerance)
            {
                var away = Mathf.Sign(player.GlobalPosition.X - GlobalPosition.X);
                player.Bump(new Vector2((away == 0 ? 1 : away) * BumpVelocity.X, BumpVelocity.Y));
                _beepTimer = BeepDuration;
            }
        }
    }

    public override void _Draw()
    {
        base._Draw();
        if (_beepTimer > 0f)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(-24f, -Size.Y / 2f - 24f), "BEEP!", HorizontalAlignment.Left, -1, 18, new Color(1f, 0.3f, 0.3f));
        }
    }
}
