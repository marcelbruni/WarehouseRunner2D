using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Elements;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class MovementTests : Node2D
{
    private const float FloorTop = 0f;
    private const float PlayerHalfHeight = 26f;

    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");

    private readonly List<string> _failures = new();
    private Node2D? _world;
    private Node2D World => _world!;
    private PlayerController _player = null!;
    private ScriptedInput _input = null!;

    public override async void _Ready()
    {
        var scenarios = new (string Name, Func<Task> Run)[]
        {
            ("Jump_HoldButton_ReachesTunedHeight", JumpHoldButtonReachesTunedHeight),
            ("Jump_ReleaseEarly_IsLowerThanFullJump", JumpReleaseEarlyIsLowerThanFullJump),
            ("DoubleJump_InAir_GoesHigherThanSingleJump", DoubleJumpInAirGoesHigherThanSingleJump),
            ("SlideJump_AfterTurnaround_IsHigherThanJump", SlideJumpAfterTurnaroundIsHigherThanJump),
            ("SlideJump_ThenDoubleJumpThenDashThenJump_AllChain", SlideJumpChainWorks),
            ("JumpDashJump_InAir_KeepsDashMomentum", JumpDashJumpKeepsDashMomentum),
            ("Rollout_AfterJumpLanding_BoostsSpeed", RolloutAfterJumpLandingBoostsSpeed),
            ("Rollout_AfterDashLanding_IsStrongerThanJumpRollout", DashRolloutIsStrongerThanJumpRollout),
            ("Rollout_PressedJustBeforeLanding_IsBufferedAndPerfect", RolloutBufferedBeforeLanding),
            ("Conveyor_StandingStill_CarriesPlayer", ConveyorCarriesStandingPlayer),
            ("Conveyor_RunningAgainstBelt_IsSlower", ConveyorAgainstBeltIsSlower),
            ("Conveyor_JumpingOff_KeepsBeltMomentum", ConveyorJumpKeepsMomentum),
            ("TiltedCrate_StandingOnIt_SlidesAndBoosts", TiltedCrateSlidesAndBoosts),
            ("TiltedConveyor_StandingOnIt_IsNotSlippery", TiltedConveyorIsNotSlippery),
            ("WallKick_AgainstWall_PushesUpAndAway", WallKickPushesUpAndAway),
            ("CoyoteJump_ShortlyAfterLeavingEdge_IsGroundJump", CoyoteJumpAfterLeavingEdge),
        };

        var failedScenarios = 0;
        foreach (var (name, run) in scenarios)
        {
            await SetUpWorld();
            var failuresBefore = _failures.Count;
            try
            {
                await run();
            }
            catch (Exception exception)
            {
                _failures.Add($"{name}: exception {exception.Message}");
            }

            var passed = _failures.Count == failuresBefore;
            failedScenarios += passed ? 0 : 1;
            GD.Print(passed ? $"PASS {name}" : $"FAIL {name}");
        }

        foreach (var failure in _failures)
        {
            GD.PrintErr(failure);
        }

        GD.Print($"{scenarios.Length - failedScenarios} / {scenarios.Length} scenarios passed");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private async Task JumpHoldButtonReachesTunedHeight()
    {
        var height = await MeasureJumpHeight(releaseAfterTicks: null);
        var expected = Mathf.Pow(_player.Tuning.JumpSpeed, 2f) / (2f * _player.Tuning.Gravity);
        Expect(nameof(JumpHoldButtonReachesTunedHeight), Mathf.Abs(height - expected) < 12f, $"height {height:0} expected ~{expected:0}");
    }

    private async Task JumpReleaseEarlyIsLowerThanFullJump()
    {
        var full = await MeasureJumpHeight(releaseAfterTicks: null);
        await SetUpWorld();
        var hop = await MeasureJumpHeight(releaseAfterTicks: 4);
        Expect(nameof(JumpReleaseEarlyIsLowerThanFullJump), hop < full * 0.7f, $"hop {hop:0} full {full:0}");
    }

    private async Task DoubleJumpInAirGoesHigherThanSingleJump()
    {
        var startY = _player.GlobalPosition.Y;
        var peak = startY;
        _input.PressJump();
        await Ticks(35, () => peak = Mathf.Min(peak, _player.GlobalPosition.Y));
        _input.PressJump();
        await Ticks(1);
        Expect("DoubleJump", _player.CurrentAirMove == AirMove.DoubleJump, $"air move {_player.CurrentAirMove}");
        await Ticks(90, () => peak = Mathf.Min(peak, _player.GlobalPosition.Y));
        var single = Mathf.Pow(_player.Tuning.JumpSpeed, 2f) / (2f * _player.Tuning.Gravity);
        Expect("DoubleJump", startY - peak > single + 60f, $"height {startY - peak:0} single {single:0}");
    }

    private async Task SlideJumpAfterTurnaroundIsHigherThanJump()
    {
        var startY = _player.GlobalPosition.Y;
        await RunTo(direction: 1, ticks: 70);
        _input.MoveX = -1f;
        await Ticks(2);
        Expect("SlideJump", _player.State == PlayerState.Skidding, $"state after turnaround {_player.State}");
        _input.PressJump();
        await Ticks(1);
        Expect("SlideJump", _player.CurrentAirMove == AirMove.SlideJump, $"air move {_player.CurrentAirMove}");
        Expect("SlideJump", _player.Velocity.X < 0f, $"vx {_player.Velocity.X:0} should point backwards");

        var peak = startY;
        await Ticks(90, () => peak = Mathf.Min(peak, _player.GlobalPosition.Y));
        var single = Mathf.Pow(_player.Tuning.JumpSpeed, 2f) / (2f * _player.Tuning.Gravity);
        Expect("SlideJump", startY - peak > single * 1.15f, $"height {startY - peak:0} single {single:0}");
    }

    private async Task SlideJumpChainWorks()
    {
        await RunTo(direction: 1, ticks: 70);
        _input.MoveX = -1f;
        await Ticks(2);
        _input.PressJump();
        await Ticks(30);
        _input.PressJump();
        await Ticks(1);
        Expect("SlideJumpChain", _player.CurrentAirMove == AirMove.DoubleJump, $"after second press {_player.CurrentAirMove}");
        await Ticks(10);
        _input.PressDash();
        await Ticks(1);
        Expect("SlideJumpChain", _player.State == PlayerState.Dashing, $"after dash press {_player.State}");
        await Ticks(30);
        Expect("SlideJumpChain", _player.State == PlayerState.Airborne && _player.CurrentAirMove == AirMove.Dash, $"after dash {_player.State}/{_player.CurrentAirMove}");
    }

    private async Task JumpDashJumpKeepsDashMomentum()
    {
        _input.PressJump();
        await Ticks(15);
        _input.MoveX = 1f;
        _input.PressDash();
        await Ticks(1);
        Expect("JumpDashJump", _player.State == PlayerState.Dashing, $"state {_player.State}");
        Expect("JumpDashJump", Mathf.IsEqualApprox(_player.Velocity.X, _player.Tuning.DashSpeed), $"dash vx {_player.Velocity.X:0}");
        await Ticks(8);
        _input.PressJump();
        await Ticks(1);
        Expect("JumpDashJump", _player.CurrentAirMove == AirMove.DoubleJump && _player.Velocity.Y < 0f, $"after jump {_player.CurrentAirMove} vy {_player.Velocity.Y:0}");
        Expect("JumpDashJump", _player.Velocity.X >= _player.Tuning.DashExitSpeed - 20f, $"vx {_player.Velocity.X:0}");
    }

    private async Task RolloutAfterJumpLandingBoostsSpeed()
    {
        var speed = await PerformRollout(useDash: false, pressEarly: false);
        Expect("JumpRollout", speed > _player.Tuning.RunSpeed * 1.2f, $"speed {speed:0}");
        Expect("JumpRollout", _player.IsBoosted, "boost not active");
    }

    private async Task DashRolloutIsStrongerThanJumpRollout()
    {
        var jumpRollout = await PerformRollout(useDash: false, pressEarly: false);
        await SetUpWorld();
        var dashRollout = await PerformRollout(useDash: true, pressEarly: false);
        Expect("DashRollout", dashRollout > jumpRollout + 20f, $"dash {dashRollout:0} jump {jumpRollout:0}");
    }

    private async Task RolloutBufferedBeforeLanding()
    {
        var perfect = false;
        _player.TrickPerformed += (trick, isPerfect) =>
        {
            if (trick == Trick.JumpRollout)
            {
                perfect = isPerfect;
            }
        };

        await PerformRollout(useDash: false, pressEarly: true);
        Expect("BufferedRollout", _player.State == PlayerState.RollingOut, $"state {_player.State}");
        Expect("BufferedRollout", perfect, "press 2 ticks before landing should count as perfect");
    }

    private async Task ConveyorCarriesStandingPlayer()
    {
        AddBelt(new Vector2(0f, FloorTop + 8f), 800f, 200f, 0f);
        await SettleAt(new Vector2(0f, FloorTop - 4f - PlayerHalfHeight));
        var startX = _player.GlobalPosition.X;
        await Ticks(60);
        var moved = _player.GlobalPosition.X - startX;
        Expect("ConveyorCarries", moved > 70f, $"moved {moved:0}");
    }

    private async Task ConveyorAgainstBeltIsSlower()
    {
        var normal = await MeasureRunDistance(beltSpeed: null);
        await SetUpWorld();
        var against = await MeasureRunDistance(beltSpeed: -180f);
        Expect("ConveyorAgainst", against < normal - 50f, $"against {against:0} normal {normal:0}");
    }

    private async Task ConveyorJumpKeepsMomentum()
    {
        AddBelt(new Vector2(0f, FloorTop + 8f), 2000f, 260f, 0f);
        await SettleAt(new Vector2(-600f, FloorTop - 4f - PlayerHalfHeight));
        _input.MoveX = 1f;
        await Ticks(60);
        _input.PressJump();
        await Ticks(3);
        Expect("ConveyorJump", _player.Velocity.X > _player.Tuning.RunSpeed + 150f, $"air vx {_player.Velocity.X:0}");
    }

    private async Task TiltedCrateSlidesAndBoosts()
    {
        AddCrate(new Vector2(0f, -150f), new Vector2(700f, 40f), Mathf.DegToRad(25f));
        await SettleAt(new Vector2(-150f, -320f), settleTicks: 0);
        var startX = _player.GlobalPosition.X;
        var slid = false;
        await Ticks(90, () => slid |= _player.State == PlayerState.SlopeSliding);
        Expect("TiltedCrate", slid, $"never slid, state {_player.State}");
        Expect("TiltedCrate", _player.GlobalPosition.X - startX > 80f, $"moved {_player.GlobalPosition.X - startX:0}");
        Expect("TiltedCrate", _player.IsBoosted, "boost not active");
    }

    private async Task TiltedConveyorIsNotSlippery()
    {
        AddBelt(new Vector2(0f, -150f), 700f, 0f, Mathf.DegToRad(-20f));
        await SettleAt(new Vector2(0f, -260f), settleTicks: 0);
        var slid = false;
        await Ticks(60, () => slid |= _player.State == PlayerState.SlopeSliding);
        Expect("TiltedConveyor", !slid, "player slid on a conveyor");
    }

    private async Task WallKickPushesUpAndAway()
    {
        AddConcrete(new Vector2(120f, -300f), new Vector2(40f, 600f));
        await SettleAt(new Vector2(40f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        _input.PressJump();
        var touched = await Until(() => _player.IsTouchingWall, 60);
        Expect("WallKick", touched, "never touched the wall");
        _input.PressJump();
        await Ticks(1);
        Expect("WallKick", _player.CurrentAirMove == AirMove.WallKick, $"air move {_player.CurrentAirMove}");
        Expect("WallKick", _player.Velocity.X < 0f && _player.Velocity.Y < 0f, $"velocity {_player.Velocity}");
    }

    private async Task CoyoteJumpAfterLeavingEdge()
    {
        await SetUpWorld(new Rect2(-1000f, FloorTop, 1000f, 200f), new Vector2(-60f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        await Until(() => _player.State == PlayerState.Airborne, 120);
        await Ticks(4);
        _input.PressJump();
        await Ticks(1);
        Expect("CoyoteJump", _player.CurrentAirMove == AirMove.Jump, $"air move {_player.CurrentAirMove}");
    }

    private async Task<float> MeasureJumpHeight(int? releaseAfterTicks)
    {
        var startY = _player.GlobalPosition.Y;
        var peak = startY;
        _input.PressJump();
        for (var tick = 0; tick < 90; tick++)
        {
            if (releaseAfterTicks is { } release && tick == release)
            {
                _input.JumpHeld = false;
            }

            await Ticks(1);
            peak = Mathf.Min(peak, _player.GlobalPosition.Y);
        }

        return startY - peak;
    }

    private async Task<float> PerformRollout(bool useDash, bool pressEarly)
    {
        await RunTo(direction: 1, ticks: 60);
        _input.PressJump();
        await Ticks(12);
        if (useDash)
        {
            _input.PressDash();
        }

        if (pressEarly)
        {
            await Until(() => _player.Velocity.Y > 0f && TicksToFloor() <= 2, 240);
            _input.PressRoll();
        }

        await Until(() => _player.IsOnGround, 240);
        if (!pressEarly)
        {
            _input.PressRoll();
        }

        await Ticks(1);
        return Mathf.Abs(_player.GroundSpeed);
    }

    private int TicksToFloor()
    {
        var feet = _player.GlobalPosition.Y + PlayerHalfHeight;
        var perTick = _player.Velocity.Y / Engine.PhysicsTicksPerSecond;
        return perTick <= 0f ? int.MaxValue : Mathf.CeilToInt((FloorTop - feet) / perTick);
    }

    private async Task<float> MeasureRunDistance(float? beltSpeed)
    {
        if (beltSpeed is { } speed)
        {
            AddBelt(new Vector2(0f, FloorTop + 8f), 2000f, speed, 0f);
        }

        var standY = beltSpeed is null ? FloorTop - PlayerHalfHeight : FloorTop - 4f - PlayerHalfHeight;
        await SettleAt(new Vector2(-600f, standY));
        var startX = _player.GlobalPosition.X;
        _input.MoveX = 1f;
        await Ticks(90);
        return _player.GlobalPosition.X - startX;
    }

    private async Task RunTo(int direction, int ticks)
    {
        _input.MoveX = direction;
        await Ticks(ticks);
    }

    private Task SetUpWorld() =>
        SetUpWorld(new Rect2(-2000f, FloorTop, 4000f, 200f), new Vector2(-600f, FloorTop - PlayerHalfHeight));

    private async Task SetUpWorld(Rect2 floor, Vector2 spawn)
    {
        if (_world is not null && IsInstanceValid(_world))
        {
            RemoveChild(_world);
            _world.QueueFree();
        }

        _world = new Node2D();
        AddChild(_world);
        AddConcrete(floor.GetCenter(), floor.Size);
        _player = SpawnPlayer(spawn);
        await Ticks(10);
    }

    private async Task SettleAt(Vector2 position, int settleTicks = 10)
    {
        _player.Respawn(position);
        await Ticks(settleTicks);
    }

    private PlayerController SpawnPlayer(Vector2 position)
    {
        var player = PlayerScene.Instantiate<PlayerController>();
        _input = new ScriptedInput();
        player.InputSource = _input;
        player.Position = position;
        World.AddChild(player);
        return player;
    }

    private void AddConcrete(Vector2 center, Vector2 size) =>
        World.AddChild(new ConcreteBlock { Position = center, Size = size });

    private void AddCrate(Vector2 center, Vector2 size, float rotation) =>
        World.AddChild(new Crate { Position = center, Size = size, Rotation = rotation });

    private void AddBelt(Vector2 center, float length, float speed, float rotation) =>
        World.AddChild(new ConveyorBelt { Position = center, Size = new Vector2(length, 24f), Rotation = rotation, BeltSpeed = speed });

    private async Task Ticks(int count, Action? afterEachTick = null)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            afterEachTick?.Invoke();
        }
    }

    private async Task<bool> Until(Func<bool> condition, int maxTicks)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            if (condition())
            {
                return true;
            }

            await Ticks(1);
        }

        return condition();
    }

    private void Expect(string scenario, bool condition, string detail)
    {
        if (!condition)
        {
            _failures.Add($"{scenario}: {detail}");
        }
    }

    private sealed class ScriptedInput : IPlayerInputSource
    {
        private bool _jumpPressed;
        private bool _dashPressed;
        private bool _rollPressed;

        public float MoveX { get; set; }

        public bool JumpHeld { get; set; }

        public void PressJump()
        {
            _jumpPressed = true;
            JumpHeld = true;
        }

        public void PressDash() => _dashPressed = true;

        public void PressRoll() => _rollPressed = true;

        public PlayerInput Sample()
        {
            var sample = new PlayerInput(MoveX, _jumpPressed, JumpHeld, _dashPressed, _rollPressed);
            _jumpPressed = false;
            _dashPressed = false;
            _rollPressed = false;
            return sample;
        }
    }
}
