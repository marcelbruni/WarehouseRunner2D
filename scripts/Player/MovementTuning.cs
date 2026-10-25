using Godot;

namespace WarehouseRunner.Player;

[GlobalClass]
public partial class MovementTuning : Resource
{
    [ExportGroup("Gravity")]
    [Export] public float Gravity { get; set; } = 2200f;
    [Export] public float MaxFallSpeed { get; set; } = 1100f;
    [Export] public float JumpReleaseGravityFactor { get; set; } = 2.4f;

    [ExportGroup("Run")]
    [Export] public float RunSpeed { get; set; } = 340f;
    [Export] public float GroundAcceleration { get; set; } = 2200f;
    [Export] public float GroundFriction { get; set; } = 2600f;
    [Export] public float TurnDeceleration { get; set; } = 3200f;
    [Export] public float OverspeedDecay { get; set; } = 420f;

    [ExportGroup("Air")]
    [Export] public float AirAcceleration { get; set; } = 1500f;
    [Export] public float AirTurnAcceleration { get; set; } = 2200f;
    [Export] public float AirOverspeedDecay { get; set; } = 120f;
    [Export] public float AirFriction { get; set; } = 350f;

    [ExportGroup("Jumps")]
    [Export] public float JumpSpeed { get; set; } = 760f;
    [Export] public float DoubleJumpSpeed { get; set; } = 680f;
    [Export] public float SlideJumpSpeed { get; set; } = 1020f;
    [Export] public float SlideJumpHorizontalSpeed { get; set; } = 170f;
    [Export] public float CoyoteTime { get; set; } = 0.1f;
    [Export] public float JumpBufferTime { get; set; } = 0.12f;

    [ExportGroup("Skid")]
    [Export] public float SkidMinSpeed { get; set; } = 170f;
    [Export] public float SkidDeceleration { get; set; } = 1400f;
    [Export] public float SkidMaxTime { get; set; } = 0.5f;
    [Export] public float SlideJumpGraceTime { get; set; } = 0.15f;

    [ExportGroup("Dash")]
    [Export] public float DashSpeed { get; set; } = 720f;
    [Export] public float DashDuration { get; set; } = 0.17f;
    [Export] public float DashExitSpeed { get; set; } = 520f;

    [ExportGroup("Rollout")]
    [Export] public float RollEarlyWindow { get; set; } = 0.15f;
    [Export] public float RollLateWindow { get; set; } = 0.12f;
    [Export] public float PerfectRollWindow { get; set; } = 0.05f;
    [Export] public float RolloutDuration { get; set; } = 0.32f;
    [Export] public float JumpRolloutBoost { get; set; } = 1.3f;
    [Export] public float JumpRolloutBoostTime { get; set; } = 0.8f;
    [Export] public float DashRolloutBoost { get; set; } = 1.45f;
    [Export] public float DashRolloutBoostTime { get; set; } = 1.0f;
    [Export] public float PerfectRollBonus { get; set; } = 0.1f;

    [ExportGroup("Ground Roll")]
    [Export] public float GroundRollMinSpeed { get; set; } = 120f;
    [Export] public float GroundRollBoost { get; set; } = 1.2f;
    [Export] public float GroundRollBoostTime { get; set; } = 0.5f;

    [ExportGroup("Roll Chain")]
    [Export] public float RollJumpHorizontalFactor { get; set; } = 1.2f;
    [Export] public float RollJumpMaxSpeed { get; set; } = 650f;
    [Export] public float ChainDashSpeed { get; set; } = 860f;
    [Export] public float ChainDashExitSpeed { get; set; } = 620f;

    [ExportGroup("Slippery Slope")]
    [Export] public float SlopeSlideGravityFactor { get; set; } = 1.1f;
    [Export] public float SlopeSteerAcceleration { get; set; } = 500f;
    [Export] public float SlopeSlideMaxSpeed { get; set; } = 820f;
    [Export] public float SlopeEntrySpeedFactor { get; set; } = 1.05f;
    [Export] public float SlopeEntryLockTime { get; set; } = 0.15f;
    [Export] public float SlopeSlideBoost { get; set; } = 1.35f;
    [Export] public float SlopeSlideBoostAfterTime { get; set; } = 0.9f;

    [ExportGroup("Wall")]
    [Export] public float WallSlideMaxFallSpeed { get; set; } = 240f;
    [Export] public float WallKickSpeed { get; set; } = 720f;
    [Export] public float WallKickHorizontalSpeed { get; set; } = 380f;
    [Export] public float WallKickInputLockTime { get; set; } = 0.14f;
    [Export] public float WallCoyoteTime { get; set; } = 0.08f;
}
