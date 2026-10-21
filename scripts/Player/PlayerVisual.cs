using Godot;
using WarehouseRunner.Elements;

namespace WarehouseRunner.Player;

public partial class PlayerVisual : Node2D
{
    private const float FootY = 26f;
    private const float DoubleJumpSpinTime = 0.4f;
    private const float SlideJumpSpinTime = 0.6f;

    private static readonly Color Vest = new(1f, 0.5f, 0.1f);
    private static readonly Color Reflector = new(0.92f, 0.96f, 0.65f);
    private static readonly Color Pants = new(0.16f, 0.22f, 0.38f);
    private static readonly Color Boots = new(0.12f, 0.1f, 0.08f);
    private static readonly Color Skin = new(0.96f, 0.78f, 0.62f);
    private static readonly Color Helmet = new(1f, 0.82f, 0.1f);
    private static readonly Color HelmetDark = new(0.7f, 0.55f, 0.05f);

    private PlayerController _player = null!;
    private CpuParticles2D _dust = null!;
    private CpuParticles2D _landingDust = null!;
    private CpuParticles2D _boostSparks = null!;
    private Vector2 _squash = Vector2.One;
    private float _runPhase;
    private float _rollAngle;

    private readonly record struct Pose(float LegSwing, float ArmSwing, float Crouch, bool ArmsUp, bool Tucked);

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _player.Landed += OnLanded;
        _player.TrickPerformed += OnTrick;

        _dust = CreateDust(oneShot: false);
        _landingDust = CreateDust(oneShot: true);
        _boostSparks = CreateSparks();
        AddChild(_dust);
        AddChild(_landingDust);
        AddChild(_boostSparks);
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _squash = _squash.Lerp(Vector2.One, Mathf.Min(1f, dt * 12f));
        _runPhase += Mathf.Abs(_player.GroundSpeed) * dt * 0.045f;
        if (_player.State == PlayerState.RollingOut)
        {
            _rollAngle += _player.GroundSpeed * dt / 16f;
        }

        Rotation = BodyRotation();
        Scale = _squash;

        var skidding = _player.State is PlayerState.Skidding or PlayerState.SlopeSliding || _player.IsWallSliding;
        _dust.Emitting = skidding;
        _dust.Direction = new Vector2(-_player.Facing, -1f);
        _boostSparks.Emitting = _player.IsBoosted;
        _boostSparks.Direction = new Vector2(-Mathf.Sign(_player.Velocity.X + 0.01f), -0.3f);

        QueueRedraw();
    }

    public override void _Draw()
    {
        var facing = _player.Facing;
        if (_player.State == PlayerState.RollingOut)
        {
            DrawBall();
            return;
        }

        DrawWorker(facing, CurrentPose());
        if (_player.CarriesPackage)
        {
            PackageArt.Draw(this, new Vector2(0f, -52f), 28f);
        }
    }

    private float BodyRotation()
    {
        var facing = _player.Facing;
        switch (_player.State)
        {
            case PlayerState.RollingOut:
                return _rollAngle;
            case PlayerState.Dashing:
                return facing * 1.25f;
            case PlayerState.Skidding:
                return -Mathf.Sign(_player.GroundSpeed) * 0.35f;
            case PlayerState.SlopeSliding:
                var normal = _player.GetFloorNormal();
                return Mathf.Atan2(normal.X, -normal.Y);
            case PlayerState.Grounded:
                return _player.GroundSpeed / _player.Tuning.RunSpeed * 0.1f;
            case PlayerState.Airborne when _player.CurrentAirMove == AirMove.DoubleJump:
                return facing * Mathf.Tau * Mathf.Clamp(_player.StateTime / DoubleJumpSpinTime, 0f, 1f);
            case PlayerState.Airborne when _player.CurrentAirMove == AirMove.SlideJump:
                return -facing * Mathf.Tau * Mathf.Clamp(_player.StateTime / SlideJumpSpinTime, 0f, 1f);
            default:
                return 0f;
        }
    }

    private Pose CurrentPose()
    {
        var carry = _player.CarriesPackage;
        return _player.State switch
        {
            PlayerState.Grounded when Mathf.Abs(_player.GroundSpeed) > 10f =>
                new Pose(Mathf.Sin(_runPhase) * 0.8f, -Mathf.Sin(_runPhase) * 0.9f, 0f, carry, false),
            PlayerState.Grounded => new Pose(0f, 0.1f, 0f, carry, false),
            PlayerState.Skidding => new Pose(-0.7f, 0.9f, 4f, carry, false),
            PlayerState.SlopeSliding => new Pose(0.6f, -1.2f, 8f, carry, false),
            PlayerState.Dashing => new Pose(-0.5f, 2.6f, 0f, carry, false),
            PlayerState.Airborne when _player.IsWallSliding => new Pose(0.3f, 2.8f, 2f, carry, false),
            PlayerState.Airborne when _player.CurrentAirMove is AirMove.DoubleJump or AirMove.SlideJump =>
                new Pose(0.4f, 1.5f, 6f, carry, true),
            PlayerState.Airborne when _player.Velocity.Y < 0f => new Pose(0.5f, 2.2f, 0f, carry, false),
            _ => new Pose(-0.3f, 1.4f, 0f, carry, false),
        };
    }

    private void DrawWorker(int facing, Pose pose)
    {
        Vector2 P(float x, float y) => new(x * facing, y);

        var hip = P(0f, 6f + pose.Crouch);
        var legLength = pose.Tucked ? 13f : 20f - (pose.Crouch * 0.5f);
        DrawLeg(hip, pose.LegSwing, legLength, facing);
        DrawLeg(hip, -pose.LegSwing, legLength, facing);

        var torso = new Rect2(-10f, -16f + pose.Crouch, 20f, 24f);
        DrawRect(torso, Vest);
        DrawRect(new Rect2(-10f, -4f + pose.Crouch, 20f, 3f), Reflector);

        var shoulder = P(0f, -12f + pose.Crouch);
        if (pose.ArmsUp)
        {
            DrawArm(shoulder, P(-6f, -38f), shoulder + P(6f, -26f));
        }
        else
        {
            DrawArm(shoulder, shoulder + (P(Mathf.Sin(pose.ArmSwing), Mathf.Cos(pose.ArmSwing)) * 16f), null);
            DrawArm(shoulder, shoulder + (P(Mathf.Sin(-pose.ArmSwing * 0.6f), Mathf.Cos(pose.ArmSwing * 0.6f)) * 16f), null);
        }

        var head = P(2f, -24f + pose.Crouch);
        DrawCircle(head, 8f, Skin);
        DrawCircle(head + P(4f, -1f), 1.6f, Colors.Black);
        DrawHelmet(head + new Vector2(0f, -2f), facing);
    }

    private void DrawLeg(Vector2 hip, float swing, float length, int facing)
    {
        var foot = hip + (new Vector2(Mathf.Sin(swing) * facing, Mathf.Cos(swing)) * length);
        DrawLine(hip, foot, Pants, 7f);
        DrawRect(new Rect2(foot + new Vector2(facing > 0 ? -3f : -7f, -2f), new Vector2(10f, 5f)), Boots);
    }

    private void DrawArm(Vector2 shoulder, Vector2 hand, Vector2? secondHand)
    {
        DrawLine(shoulder, hand, Vest, 5f);
        DrawCircle(hand, 3f, Skin);
        if (secondHand is { } other)
        {
            DrawLine(shoulder, other, Vest, 5f);
            DrawCircle(other, 3f, Skin);
        }
    }

    private void DrawHelmet(Vector2 center, int facing)
    {
        var points = new Vector2[10];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = Mathf.Pi + (Mathf.Pi * i / (points.Length - 1));
            points[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 9.5f);
        }

        DrawColoredPolygon(points, Helmet);
        DrawLine(center + new Vector2(-10f * facing, 0f), center + new Vector2(13f * facing, 0f), HelmetDark, 3f);
    }

    private void DrawBall()
    {
        DrawCircle(Vector2.Zero, 17f, Vest);
        DrawLine(new Vector2(-17f, 0f), new Vector2(17f, 0f), Reflector, 3f);
        DrawCircle(new Vector2(0f, -10f), 7f, Helmet);
        DrawCircle(new Vector2(0f, 12f), 5f, Pants);
    }

    private void OnLanded(float impactSpeed)
    {
        var strength = Mathf.Clamp(impactSpeed / 1000f, 0f, 1f);
        _squash = new Vector2(1f + (0.3f * strength), 1f - (0.3f * strength));
        if (strength > 0.3f)
        {
            _landingDust.Restart();
        }
    }

    private void OnTrick(Trick trick, bool perfect)
    {
        _squash = trick switch
        {
            Trick.Jump or Trick.DoubleJump or Trick.SlideJump or Trick.DashJump or Trick.WallKick => new Vector2(0.8f, 1.25f),
            Trick.Dash => new Vector2(1.3f, 0.75f),
            _ => _squash,
        };
    }

    private static CpuParticles2D CreateDust(bool oneShot)
    {
        var fade = new Gradient();
        fade.SetColor(0, new Color(0.8f, 0.78f, 0.72f, 0.7f));
        fade.SetColor(1, new Color(0.8f, 0.78f, 0.72f, 0f));
        return new CpuParticles2D
        {
            Position = new Vector2(0f, FootY),
            Amount = oneShot ? 14 : 10,
            Lifetime = 0.45f,
            OneShot = oneShot,
            Explosiveness = oneShot ? 1f : 0f,
            Emitting = false,
            LocalCoords = false,
            Direction = new Vector2(0f, -1f),
            Spread = oneShot ? 80f : 35f,
            InitialVelocityMin = 40f,
            InitialVelocityMax = 130f,
            Gravity = new Vector2(0f, 220f),
            ScaleAmountMin = 3f,
            ScaleAmountMax = 6f,
            ColorRamp = fade,
        };
    }

    private static CpuParticles2D CreateSparks()
    {
        var fade = new Gradient();
        fade.SetColor(0, new Color(1f, 0.85f, 0.3f, 1f));
        fade.SetColor(1, new Color(1f, 0.35f, 0.1f, 0f));
        return new CpuParticles2D
        {
            Amount = 24,
            Lifetime = 0.35f,
            Emitting = false,
            LocalCoords = false,
            Spread = 25f,
            InitialVelocityMin = 60f,
            InitialVelocityMax = 160f,
            Gravity = Vector2.Zero,
            ScaleAmountMin = 2f,
            ScaleAmountMax = 3.5f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(6f, 22f),
            ColorRamp = fade,
        };
    }
}
