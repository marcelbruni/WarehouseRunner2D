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
            ("SlideJump_BackThenForwardAgain_StaysSkiddingAndTriggers", SlideJumpBackThenForwardTriggers),
            ("JumpDashJump_InAir_KeepsDashMomentum", JumpDashJumpKeepsDashMomentum),
            ("Rollout_AfterJumpLanding_BoostsSpeed", RolloutAfterJumpLandingBoostsSpeed),
            ("Rollout_AfterDashLanding_IsStrongerThanJumpRollout", DashRolloutIsStrongerThanJumpRollout),
            ("Rollout_PressedJustBeforeLanding_IsBufferedAndPerfect", RolloutBufferedBeforeLanding),
            ("GroundRoll_WhileRunning_IsSlightlySlowerThanRollout", GroundRollWhileRunningIsSlowerThanRollout),
            ("GroundRoll_WhenStanding_DoesNothing", GroundRollWhenStandingDoesNothing),
            ("GroundRoll_AgainDuringCooldown_IsBlocked", GroundRollBlockedDuringCooldown),
            ("GroundRoll_AtVeryHighSpeed_Works", GroundRollAtVeryHighSpeed),
            ("RollJump_OutOfRoll_FliesFartherThanRunningJump", RollJumpFliesFarther),
            ("PowerDash_AfterRollJump_IsFasterThanDash", PowerDashAfterRollJumpIsFaster),
            ("SpeedLoop_RollJumpDash_SustainsHighSpeed", () => SpeedLoopSustainsHighSpeed(startWithRoll: true)),
            ("SpeedLoop_JumpDashRoll_SustainsHighSpeed", () => SpeedLoopSustainsHighSpeed(startWithRoll: false)),
            ("LowShelf_Rolling_PassesUnderneath", () => LowShelfPassage(roll: true)),
            ("LowShelf_Running_IsBlocked", () => LowShelfPassage(roll: false)),
            ("LowShelf_JumpWhileUnderneath_KeepsRolling", LowShelfJumpKeepsRolling),
            ("Conveyor_StandingStill_CarriesPlayer", ConveyorCarriesStandingPlayer),
            ("Conveyor_RunningAgainstBelt_IsSlower", ConveyorAgainstBeltIsSlower),
            ("Conveyor_JumpingOff_KeepsBeltMomentum", ConveyorJumpKeepsMomentum),
            ("TiltedCrate_StandingOnIt_SlidesAndBoosts", TiltedCrateSlidesAndBoosts),
            ("TiltedCrate_LandingFast_KeepsHorizontalSpeed", () => TiltedCrateKeepsEntrySpeed(600f)),
            ("TiltedCrate_LandingFasterThanSlideMax_KeepsHorizontalSpeed", () => TiltedCrateKeepsEntrySpeed(950f)),
            ("TiltedCrate_LandingUphill_GetsNoSpeedBonus", TiltedCrateUphillGivesNoBonus),
            ("TiltedConveyor_StandingOnIt_IsNotSlippery", TiltedConveyorIsNotSlippery),
            ("WallKick_SteeringAwayFromWall_PushesUpAndAway", WallKickPushesUpAndAway),
            ("WallJump_SteeringIntoWall_IsDoubleJump", JumpSteeringIntoWallIsDoubleJump),
            ("WallJump_NoSteering_IsDoubleJump", JumpNeutralAtWallIsDoubleJump),
            ("WallKick_AfterDoubleJump_RefreshesDoubleJump", WallKickRefreshesDoubleJump),
            ("CoyoteJump_ShortlyAfterLeavingEdge_IsGroundJump", CoyoteJumpAfterLeavingEdge),
            ("Dash_FallingAfterWalkingOffEdge_Works", () => DashWhileFallingOffEdge(dropHeight: 200f, ticksBeforeDash: 10)),
            ("Dash_FallingOffLowCrate_Works", () => DashWhileFallingOffEdge(dropHeight: 70f, ticksBeforeDash: 8)),
            ("Dash_FallingOffLowCrateShortlyBeforeLanding_Works", () => DashWhileFallingOffEdge(dropHeight: 70f, ticksBeforeDash: 15)),
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

    private async Task SlideJumpBackThenForwardTriggers()
    {
        await RunTo(direction: 1, ticks: 70);
        _input.MoveX = -1f;
        await Ticks(3);
        _input.MoveX = 0f;
        await Ticks(1);
        _input.MoveX = 1f;
        await Ticks(3);
        Expect("SlideJumpBackForward", _player.State == PlayerState.Skidding, $"state after back and forward {_player.State}");
        _input.PressJump();
        await Ticks(1);
        Expect("SlideJumpBackForward", _player.CurrentAirMove == AirMove.SlideJump, $"air move {_player.CurrentAirMove}");
        Expect("SlideJumpBackForward", _player.Velocity.X > 0f, $"vx {_player.Velocity.X:0} should follow the forward input");
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
        _input.PressDashOrRoll();
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
        _input.PressDashOrRoll();
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

        var dashed = false;
        _player.TrickPerformed += (trick, _) => dashed |= trick == Trick.Dash;
        await PerformRollout(useDash: false, pressEarly: true);
        Expect("BufferedRollout", !dashed, "press just before landing must not dash");
        Expect("BufferedRollout", _player.State == PlayerState.RollingOut, $"state {_player.State}");
        Expect("BufferedRollout", perfect, "press 2 ticks before landing should count as perfect");
    }

    private async Task GroundRollWhileRunningIsSlowerThanRollout()
    {
        var rollout = await PerformRollout(useDash: false, pressEarly: false);
        await SetUpWorld();
        await RunTo(direction: 1, ticks: 60);
        _input.PressDashOrRoll();
        await Ticks(1);
        var roll = Mathf.Abs(_player.GroundSpeed);
        Expect("GroundRoll", _player.State == PlayerState.RollingOut, $"state {_player.State}");
        Expect("GroundRoll", roll > _player.Tuning.RunSpeed * 1.1f && roll < rollout, $"roll {roll:0} rollout {rollout:0}");
    }

    private async Task GroundRollBlockedDuringCooldown()
    {
        var cooldownTicks = Mathf.CeilToInt(_player.Tuning.GroundRollCooldown * Engine.PhysicsTicksPerSecond);
        var rolloutTicks = Mathf.CeilToInt(_player.Tuning.RolloutDuration * Engine.PhysicsTicksPerSecond);
        await RunTo(direction: 1, ticks: 60);
        _input.PressDashOrRoll();
        await Ticks(rolloutTicks + 3);
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("GroundRollCooldown", _player.State != PlayerState.RollingOut, $"second roll during cooldown: state {_player.State}");
        await Ticks(cooldownTicks - rolloutTicks);
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("GroundRollCooldown", _player.State == PlayerState.RollingOut, $"roll after cooldown: state {_player.State}, speed {_player.GroundSpeed:0}");
    }

    private async Task GroundRollAtVeryHighSpeed()
    {
        _input.MoveX = 1f;
        _player.Launch(new Vector2(950f, 0f));
        await Ticks(30);
        Expect("GroundRollFast", _player.State == PlayerState.Grounded, $"not running before roll: state {_player.State}");
        var speedBefore = _player.GroundSpeed;
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("GroundRollFast", _player.State == PlayerState.RollingOut, $"no roll at speed {speedBefore:0}: state {_player.State}");
        Expect("GroundRollFast", _player.GroundSpeed >= speedBefore - 10f, $"roll lost speed: {speedBefore:0} -> {_player.GroundSpeed:0}");

        // A second roll while still far above run speed must work once the cooldown is over.
        await Until(() => _player.State == PlayerState.Grounded, 120);
        await Ticks(Mathf.CeilToInt(_player.Tuning.GroundRollCooldown * Engine.PhysicsTicksPerSecond));
        var speedAgain = _player.GroundSpeed;
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("GroundRollFast", speedAgain > _player.Tuning.RunSpeed * 1.3f, $"test setup: speed {speedAgain:0} already back near run speed");
        Expect("GroundRollFast", _player.State == PlayerState.RollingOut, $"no second roll at speed {speedAgain:0}: state {_player.State}");
    }

    private async Task GroundRollWhenStandingDoesNothing()
    {
        _input.PressDashOrRoll();
        await Ticks(2);
        Expect("GroundRollStanding", _player.State == PlayerState.Grounded, $"state {_player.State}");
    }

    private async Task RollJumpFliesFarther()
    {
        var normal = await MeasureJumpDistance(fromRoll: false);
        await SetUpWorld();
        var fromRoll = await MeasureJumpDistance(fromRoll: true);
        Expect("RollJump", fromRoll > normal * 1.3f, $"roll jump {fromRoll:0} running jump {normal:0}");
    }

    private async Task PowerDashAfterRollJumpIsFaster()
    {
        await RunTo(direction: 1, ticks: 60);
        _input.PressDashOrRoll();
        await Ticks(3);
        _input.PressJump();
        await Ticks(15);
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("PowerDash", _player.State == PlayerState.Dashing, $"state {_player.State}");
        Expect("PowerDash", _player.Velocity.X > _player.Tuning.DashSpeed + 50f, $"dash vx {_player.Velocity.X:0}");
    }

    private async Task SpeedLoopSustainsHighSpeed(bool startWithRoll)
    {
        await SetUpWorld(new Rect2(-3000f, FloorTop, 12000f, 200f), new Vector2(-2800f, FloorTop - PlayerHalfHeight));
        await RunTo(direction: 1, ticks: 40);
        var landingSpeeds = new List<float>();
        for (var cycle = 0; cycle < 5; cycle++)
        {
            if (cycle > 0 || startWithRoll)
            {
                _input.PressDashOrRoll();
                await Ticks(3);
                Expect("SpeedLoop", _player.State == PlayerState.RollingOut, $"cycle {cycle}: state {_player.State} instead of roll");
            }

            _input.PressJump();
            await Ticks(15);
            _input.PressDashOrRoll();
            await Ticks(1);
            Expect("SpeedLoop", _player.State == PlayerState.Dashing, $"cycle {cycle}: state {_player.State} instead of dash");
            await Until(() => _player.IsOnGround, 240);
            landingSpeeds.Add(Mathf.Abs(_player.GroundSpeed));
        }

        var steady = landingSpeeds.GetRange(1, landingSpeeds.Count - 1);
        var min = Mathf.Inf;
        var max = 0f;
        foreach (var speed in steady)
        {
            min = Mathf.Min(min, speed);
            max = Mathf.Max(max, speed);
        }

        var summary = string.Join(", ", landingSpeeds.ConvertAll(v => v.ToString("0")));
        GD.Print($"  landing speeds: {summary}");
        Expect("SpeedLoop", min > _player.Tuning.RunSpeed * 1.5f, $"landing speeds {summary}");
        Expect("SpeedLoop", max - min < 60f, $"speed not constant: {summary}");
    }

    private async Task<float> MeasureJumpDistance(bool fromRoll)
    {
        await RunTo(direction: 1, ticks: 60);
        if (fromRoll)
        {
            _input.PressDashOrRoll();
            await Ticks(3);
        }

        var startX = _player.GlobalPosition.X;
        _input.PressJump();
        await Ticks(2);
        await Until(() => _player.IsOnGround, 240);
        return _player.GlobalPosition.X - startX;
    }

    private async Task LowShelfPassage(bool roll)
    {
        AddCrate(new Vector2(0f, -220f), new Vector2(400f, 360f));
        await SettleAt(new Vector2(-500f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        await Until(() => _player.GlobalPosition.X > -260f, 120);
        if (roll)
        {
            _input.PressDashOrRoll();
        }

        await Ticks(240);
        var passed = _player.GlobalPosition.X > 230f;
        Expect("LowShelf", passed == roll, $"x {_player.GlobalPosition.X:0}, state {_player.State}, curled {_player.IsCurledUp}");
        if (roll)
        {
            Expect("LowShelf", !_player.IsCurledUp && _player.State == PlayerState.Grounded, $"after shelf: state {_player.State}, curled {_player.IsCurledUp}");
        }
    }

    private async Task LowShelfJumpKeepsRolling()
    {
        AddCrate(new Vector2(0f, -220f), new Vector2(400f, 360f));
        await SettleAt(new Vector2(-500f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        await Until(() => _player.GlobalPosition.X > -260f, 120);
        _input.PressDashOrRoll();
        await Until(() => _player.GlobalPosition.X > 0f, 120);
        _input.PressJump();
        await Ticks(2);
        Expect("LowShelfJump", _player.State == PlayerState.RollingOut && _player.IsCurledUp, $"state {_player.State}, curled {_player.IsCurledUp}");
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

    private async Task TiltedCrateKeepsEntrySpeed(float entrySpeed)
    {
        var scenario = $"TiltedCrateEntry{entrySpeed:0}";
        AddCrate(new Vector2(0f, -150f), new Vector2(900f, 40f), Mathf.DegToRad(25f));
        await SettleAt(new Vector2(-330f, -370f), settleTicks: 0);
        _input.MoveX = 1f;
        _player.Launch(new Vector2(entrySpeed, 0f));

        var arrivalSpeed = entrySpeed;
        var landed = false;
        for (var i = 0; i < 60 && !landed; i++)
        {
            arrivalSpeed = _player.Velocity.X;
            await Ticks(1);
            landed = _player.State == PlayerState.SlopeSliding;
        }

        Expect(scenario, landed, $"never slid, state {_player.State}");
        var startX = _player.GlobalPosition.X;
        const int measureTicks = 12;
        await Ticks(measureTicks);
        var pace = (_player.GlobalPosition.X - startX) * Engine.PhysicsTicksPerSecond / measureTicks;
        Expect(scenario, pace >= arrivalSpeed, $"horizontal pace {pace:0} after landing, arrived with {arrivalSpeed:0}");

        // Slope gravity and steering add roughly 140 px/s over the measured ticks; the entry itself should only add a little.
        Expect(scenario, pace <= (arrivalSpeed * 1.1f) + 150f, $"horizontal pace {pace:0} jumped too far above arrival {arrivalSpeed:0}");
    }

    private async Task TiltedCrateUphillGivesNoBonus()
    {
        const string scenario = "TiltedCrateUphill";
        AddCrate(new Vector2(0f, -150f), new Vector2(900f, 40f), Mathf.DegToRad(25f));
        await SettleAt(new Vector2(330f, -120f), settleTicks: 0);
        _input.MoveX = -1f;
        _player.Launch(new Vector2(-900f, 0f));

        var arrivalSpeed = 900f;
        var landed = false;
        for (var i = 0; i < 60 && !landed; i++)
        {
            arrivalSpeed = -_player.Velocity.X;
            await Ticks(1);
            landed = _player.State == PlayerState.SlopeSliding;
        }

        Expect(scenario, landed, $"never slid, state {_player.State}");
        Expect(scenario, Mathf.Abs(_player.GroundSpeed) <= arrivalSpeed, $"slide speed {Mathf.Abs(_player.GroundSpeed):0} above arrival {arrivalSpeed:0}");
        var startX = _player.GlobalPosition.X;
        const int measureTicks = 12;
        await Ticks(measureTicks);
        var pace = (startX - _player.GlobalPosition.X) * Engine.PhysicsTicksPerSecond / measureTicks;
        Expect(scenario, pace < arrivalSpeed, $"uphill pace {pace:0} not below arrival {arrivalSpeed:0}");
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
        _input.MoveX = -1f;
        _input.PressJump();
        await Ticks(1);
        Expect("WallKick", _player.CurrentAirMove == AirMove.WallKick, $"air move {_player.CurrentAirMove}");
        Expect("WallKick", _player.Velocity.X < 0f && _player.Velocity.Y < 0f, $"velocity {_player.Velocity}");
    }

    private async Task JumpSteeringIntoWallIsDoubleJump()
    {
        AddConcrete(new Vector2(120f, -300f), new Vector2(40f, 600f));
        await SettleAt(new Vector2(40f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        _input.PressJump();
        await Until(() => _player.IsTouchingWall, 60);
        _input.PressJump();
        await Ticks(1);
        Expect("JumpIntoWall", _player.CurrentAirMove == AirMove.DoubleJump, $"air move {_player.CurrentAirMove}");
    }

    private async Task WallKickRefreshesDoubleJump()
    {
        AddConcrete(new Vector2(120f, -300f), new Vector2(40f, 600f));
        await SettleAt(new Vector2(40f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        _input.PressJump();
        await Ticks(10);
        _input.PressJump();
        await Until(() => _player.IsTouchingWall, 60);
        _input.MoveX = -1f;
        _input.PressJump();
        await Ticks(20);
        _input.PressJump();
        await Ticks(1);
        Expect("WallKickRefresh", _player.CurrentAirMove == AirMove.DoubleJump, $"air move after kick + jump {_player.CurrentAirMove}");
    }

    private async Task JumpNeutralAtWallIsDoubleJump()
    {
        AddConcrete(new Vector2(120f, -300f), new Vector2(40f, 600f));
        await SettleAt(new Vector2(40f, FloorTop - PlayerHalfHeight));
        _input.MoveX = 1f;
        _input.PressJump();
        await Until(() => _player.IsTouchingWall, 60);
        _input.MoveX = 0f;
        _input.PressJump();
        await Ticks(1);
        Expect("JumpNeutralAtWall", _player.CurrentAirMove == AirMove.DoubleJump, $"air move {_player.CurrentAirMove}");
    }

    private async Task DashWhileFallingOffEdge(float dropHeight, int ticksBeforeDash)
    {
        await SetUpWorld(new Rect2(-1000f, FloorTop, 1000f, 200f), new Vector2(-60f, FloorTop - PlayerHalfHeight));
        AddConcrete(new Vector2(1500f, FloorTop + dropHeight + 100f), new Vector2(3000f, 200f));
        _input.MoveX = 1f;
        await Until(() => _player.State == PlayerState.Airborne, 120);
        await Ticks(ticksBeforeDash);
        _input.PressDashOrRoll();
        await Ticks(1);
        Expect("DashFalling", _player.State == PlayerState.Dashing, $"state {_player.State}, air move {_player.CurrentAirMove}, vy {_player.Velocity.Y:0}");
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
            _input.PressDashOrRoll();
        }

        if (pressEarly)
        {
            await Until(() => _player.Velocity.Y > 0f && TicksToFloor() <= 2, 240);
            _input.PressDashOrRoll();
        }

        await Until(() => _player.IsOnGround, 240);
        if (!pressEarly)
        {
            _input.PressDashOrRoll();
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

    private void AddCrate(Vector2 center, Vector2 size, float rotation = 0f) =>
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
        private bool _actionPressed;

        public float MoveX { get; set; }

        public bool JumpHeld { get; set; }

        public void PressJump()
        {
            _jumpPressed = true;
            JumpHeld = true;
        }

        public void PressDashOrRoll() => _actionPressed = true;

        public PlayerInput Sample()
        {
            var sample = new PlayerInput(MoveX, _jumpPressed, JumpHeld, _actionPressed);
            _jumpPressed = false;
            _actionPressed = false;
            return sample;
        }
    }
}
