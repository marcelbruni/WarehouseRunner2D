using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class LevelSkillProofs : Node
{
    private const uint ProbeLayer = 2;
    private const float StandY = -26f;

    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");

    private const string OnlyArgument = "--only=";

    private readonly List<string> _failures = new();
    private string? _only;

    private enum Opener
    {
        WalkOff,
        RunningJump,
        SlideJump,
        RollJump,
        Roll,
        JumpThenRoll,
    }

    private sealed record Plan(Opener Opener, float StartX, float StartY, float TriggerX, int RunBackTicks, int? DoubleJumpTick, int? DashTick)
    {
        public string Describe() => $"{Opener} start {StartX:0} trigger {TriggerX:0} back {RunBackTicks} dj {DoubleJumpTick?.ToString() ?? "-"} dash {DashTick?.ToString() ?? "-"}";
    }

    private sealed record Section(string Name, string ScenePath, Func<PlayerController, bool> Reached, int Ticks, float DiesBelowY = float.MaxValue);

    public override async void _Ready()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(OnlyArgument))
            {
                _only = argument[OnlyArgument.Length..];
            }
        }

        await ProveLevel01();
        await ProveLevel02();
        await ProveLevel03();
        await ProveLevel04();

        foreach (var failure in _failures)
        {
            GD.PrintErr(failure);
        }

        GD.Print(_failures.Count == 0 ? "PASS LevelSkillProofs" : "FAIL LevelSkillProofs");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private async Task ProveLevel01()
    {
        const string scene = "res://scenes/levels/Level01.tscn";

        var lowCrate = new Section("L1 Kiste 100 (Sprung)", scene, p => p.GlobalPosition.X > 300f && p.IsOnGround, 240);
        await Prove(lowCrate, required: Jumps(Opener.RunningJump, 0f, 60f, 130f, withDoubleJump: false, withDash: false),
            insufficient: [new Plan(Opener.WalkOff, 0f, StandY, 0f, 0, null, null)]);

        var highCrate = new Section("L1 Kiste 200 (Doppelsprung)", scene, p => p.GlobalPosition.X > 800f && p.IsOnGround, 300);
        await Prove(highCrate, required: Jumps(Opener.RunningJump, 450f, 520f, 620f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 450f, 500f, 640f, withDoubleJump: false, withDash: false));

        var ledge = new Section("L1 Kante 290 (Slide Jump)", scene, p => p.GlobalPosition.Y < -300f && p.IsOnGround, 360);
        await Prove(ledge, required: SlideJumps(1300f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 900f, 1100f, 1340f, withDoubleJump: true, withDash: false)
                .Concat(SlideJumps(1300f, withDoubleJump: false, withDash: false)));

        var chimney = new Section("L1 Kamin 560 (Wall Kick)", scene, p => p.GlobalPosition.Y < -570f && p.IsOnGround, 900);
        await ProveWallClimb(chimney);
    }

    private async Task ProveLevel02()
    {
        const string scene = "res://scenes/levels/Level02.tscn";

        var tunnelGap = new Section("L2 Lücke unter Decke (Sprung + Dash)", scene, p => p.GlobalPosition.X > 550f && p.IsOnGround && p.GlobalPosition.Y < 0f, 300, DiesBelowY: 40f);
        await Prove(tunnelGap, required: Jumps(Opener.RunningJump, 0f, 200f, 300f, withDoubleJump: false, withDash: true),
            insufficient: Jumps(Opener.RunningJump, 0f, 200f, 300f, withDoubleJump: true, withDash: false)
                .Append(new Plan(Opener.WalkOff, 0f, StandY, 0f, 0, null, null)));

        var wideGap = new Section("L2 Lücke 600 (Doppelsprung + Dash)", scene, p => p.GlobalPosition.X > 1720f && p.IsOnGround && p.GlobalPosition.Y < 0f, 360);
        await Prove(wideGap, required: Jumps(Opener.RunningJump, 750f, 1000f, 1100f, withDoubleJump: true, withDash: true),
            insufficient: Jumps(Opener.RunningJump, 750f, 1000f, 1100f, withDoubleJump: true, withDash: false)
                .Concat(Jumps(Opener.RunningJump, 750f, 1000f, 1100f, withDoubleJump: false, withDash: true))
                .Concat(SlideJumps(1050f, withDoubleJump: true, withDash: false))
                .Concat(SlideJumps(1050f, withDoubleJump: false, withDash: true)));

        var ledge = new Section("L2 Kante 300 (Slide Jump + Doppelsprung)", scene, p => p.GlobalPosition.Y < -310f && p.IsOnGround, 360);
        await Prove(ledge, required: SlideJumps(2330f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 1900f, 2150f, 2370f, withDoubleJump: true, withDash: false));
    }

    private async Task ProveLevel03()
    {
        const string scene = "res://scenes/levels/Level03.tscn";
        foreach (var (name, startX, shelfStart, shelfEnd) in new[] { ("L3 Regal 1 (Rolle)", -100f, 200f, 500f), ("L3 Regal 3 (Rolle)", 1750f, 1950f, 2550f) })
        {
            var shelf = new Section(name, scene, p => p.GlobalPosition.X > shelfEnd + 20f, 400);
            await Prove(shelf, required: [new Plan(Opener.Roll, startX, StandY, shelfStart - 60f, 0, null, null)],
                insufficient: Jumps(Opener.RunningJump, startX, startX + 20f, shelfStart - 20f, withDoubleJump: true, withDash: false)
                    .Append(new Plan(Opener.WalkOff, startX, StandY, 0f, 0, null, null)));
        }

        var shelfAfterCrate = new Section("L3 Regal 2 nach Kiste (Rollout)", scene, p => p.GlobalPosition.X > 1670f, 400);
        await Prove(shelfAfterCrate, required: Jumps(Opener.JumpThenRoll, 700f, 800f, 880f, withDoubleJump: false, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 700f, 800f, 880f, withDoubleJump: true, withDash: false));
    }

    private async Task ProveLevel04()
    {
        const string scene = "res://scenes/levels/Level04.tscn";
        foreach (var (name, startX, edgeX, landX) in new[] { ("L4 Lücke A 840 (Roll Jump + Power-Dash + Sprung)", -150f, 400f, 1250f), ("L4 Lücke B 840", 1260f, 1740f, 2590f) })
        {
            var gap = new Section(name, scene, p => p.GlobalPosition.X > landX && p.IsOnGround && p.GlobalPosition.Y < 0f, 420);
            await Prove(gap, required: Jumps(Opener.RollJump, startX, (edgeX - 120f), (edgeX - 10f), withDoubleJump: true, withDash: true),
                insufficient: Jumps(Opener.RunningJump, startX, (edgeX - 120f), (edgeX - 5f), withDoubleJump: true, withDash: true)
                    .Concat(SlideJumps(edgeX - 40f, withDoubleJump: true, withDash: true)));
        }
    }

    private static IEnumerable<Plan> Jumps(Opener opener, float startX, float firstTrigger, float lastTrigger, bool withDoubleJump, bool withDash)
    {
        for (var trigger = firstTrigger; trigger <= lastTrigger; trigger += 20f)
        {
            foreach (var (dj, dash) in AirTimings(withDoubleJump, withDash))
            {
                yield return new Plan(opener, startX, StandY, trigger, 0, dj, dash);
            }
        }
    }

    private static IEnumerable<Plan> SlideJumps(float startX, bool withDoubleJump, bool withDash)
    {
        foreach (var back in new[] { 15, 25, 35, 45 })
        {
            foreach (var (dj, dash) in AirTimings(withDoubleJump, withDash))
            {
                yield return new Plan(Opener.SlideJump, startX, StandY, 0f, back, dj, dash);
            }
        }
    }

    private static IEnumerable<(int? DoubleJump, int? Dash)> AirTimings(bool withDoubleJump, bool withDash)
    {
        if (!withDoubleJump && !withDash)
        {
            yield return (null, null);
            yield break;
        }

        if (withDoubleJump && !withDash)
        {
            for (var dj = 6; dj <= 78; dj += 6)
            {
                yield return (dj, null);
            }

            yield break;
        }

        if (withDash && !withDoubleJump)
        {
            for (var dash = 2; dash <= 78; dash += 4)
            {
                yield return (null, dash);
            }

            yield break;
        }

        for (var first = 6; first <= 66; first += 6)
        {
            for (var second = first + 4; second <= first + 44; second += 8)
            {
                yield return (first, second);
                yield return (second, first);
            }
        }
    }

    private bool IsSelected(Section section) => _only is null || section.Name.StartsWith(_only);

    private async Task Prove(Section section, IEnumerable<Plan> required, IEnumerable<Plan> insufficient)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var requiredResults = await Run(section, required.ToList());
        var insufficientResults = await Run(section, insufficient.ToList());

        var requiredSuccesses = requiredResults.Count(r => r.Success);
        var cheats = insufficientResults.Where(r => r.Success).ToList();
        GD.Print($"{section.Name}: gefordert {requiredSuccesses}/{requiredResults.Count} geschafft, schwächer {cheats.Count}/{insufficientResults.Count} geschafft");

        if (requiredSuccesses == 0)
        {
            _failures.Add($"{section.Name}: geforderte Technik schafft es nie");
        }

        foreach (var cheat in cheats.Take(3))
        {
            _failures.Add($"{section.Name}: schwächere Technik schafft es: {cheat.Plan.Describe()}");
        }
    }

    private async Task ProveWallClimb(Section section)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var level = LoadLevel(section.ScenePath);
        var probe = SpawnProbe(level, new Vector2(2300f, StandY));
        var input = (ScriptedInput)probe.InputSource;
        var success = false;
        var direction = 1f;
        input.MoveX = direction;
        input.PressJump();
        for (var tick = 0; tick < section.Ticks && !success; tick++)
        {
            await NextTick();
            if (probe.IsTouchingWall && probe.State == PlayerState.Airborne)
            {
                direction = -direction;
                input.MoveX = direction;
                input.PressJump();
            }
            else if (probe.State == PlayerState.Airborne && probe.Velocity.Y > 0f && probe.GlobalPosition.Y > -200f)
            {
                input.PressJump();
            }

            success = section.Reached(probe);
        }

        GD.Print($"{section.Name}: Wall-Kick-Bot {(success ? "oben angekommen" : "gescheitert")}");
        if (!success)
        {
            _failures.Add($"{section.Name}: Wall-Kick-Bot kommt nicht hoch (y {probe.GlobalPosition.Y:0})");
        }

        UnloadLevel(level);
    }

    private sealed record Result(Plan Plan, bool Success);

    private async Task<List<Result>> Run(Section section, List<Plan> plans)
    {
        var level = LoadLevel(section.ScenePath);
        var probes = plans.Select(plan => (Plan: plan, Player: SpawnProbe(level, new Vector2(plan.StartX, plan.StartY)), State: new ProbeState())).ToList();
        for (var tick = 0; tick < section.Ticks; tick++)
        {
            foreach (var probe in probes.Where(p => !p.State.Success && !p.State.Dead))
            {
                Drive(probe.Plan, probe.Player, probe.State, tick);
            }

            await NextTick();
            foreach (var probe in probes.Where(p => !p.State.Success && !p.State.Dead))
            {
                probe.State.Dead = probe.Player.GlobalPosition.Y - StandY > section.DiesBelowY;
                probe.State.Success = !probe.State.Dead && section.Reached(probe.Player);
            }
        }

        UnloadLevel(level);
        return probes.Select(p => new Result(p.Plan, p.State.Success)).ToList();
    }

    private sealed class ProbeState
    {
        public bool Success { get; set; }
        public bool Dead { get; set; }
        public int TakeoffTick { get; set; } = -1;
        public int TriggerTick { get; set; } = -1;
    }

    private static void Drive(Plan plan, PlayerController player, ProbeState state, int tick)
    {
        var input = (ScriptedInput)player.InputSource;
        if (state.TakeoffTick >= 0)
        {
            input.MoveX = 1f;
            var flight = tick - state.TakeoffTick;
            if (flight == plan.DoubleJumpTick)
            {
                input.PressJump();
            }

            if (flight == plan.DashTick)
            {
                input.PressAction();
            }

            return;
        }

        switch (plan.Opener)
        {
            case Opener.WalkOff:
                input.MoveX = 1f;
                return;
            case Opener.RunningJump:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                break;
            case Opener.SlideJump:
                input.MoveX = tick < plan.RunBackTicks ? -1f : 1f;
                if (tick == plan.RunBackTicks + 3)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                break;
            case Opener.RollJump:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressAction();
                }

                if (state.TriggerTick >= 0 && tick == state.TriggerTick + 3)
                {
                    input.PressJump();
                }

                break;
            case Opener.JumpThenRoll:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                if (state.TriggerTick >= 0 && player.Velocity.Y > 0f && player.GlobalPosition.Y > -70f)
                {
                    input.PressAction();
                }

                return;
            case Opener.Roll:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && player.State == PlayerState.Grounded)
                {
                    input.PressAction();
                }

                return;
        }

        if (state.TriggerTick >= 0 && player.State == PlayerState.Airborne)
        {
            state.TakeoffTick = tick;
        }
    }

    private Node LoadLevel(string scenePath)
    {
        var level = GD.Load<PackedScene>(scenePath).Instantiate();
        AddChild(level);
        var originalPlayer = level.GetNode<PlayerController>("Player");
        originalPlayer.ProcessMode = ProcessModeEnum.Disabled;
        originalPlayer.CollisionLayer = 0;
        originalPlayer.GlobalPosition = new Vector2(-100000f, -100000f);
        return level;
    }

    private void UnloadLevel(Node level)
    {
        RemoveChild(level);
        level.QueueFree();
    }

    private static PlayerController SpawnProbe(Node level, Vector2 position)
    {
        var player = PlayerScene.Instantiate<PlayerController>();
        foreach (var name in new[] { "Visual", "Trail", "Camera" })
        {
            var child = player.GetNode(name);
            player.RemoveChild(child);
            child.Free();
        }

        player.InputSource = new ScriptedInput();
        player.CollisionLayer = ProbeLayer;
        player.CollisionMask = 1;
        player.Position = position;
        level.AddChild(player);
        return player;
    }

    private async Task NextTick() => await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

    private sealed class ScriptedInput : IPlayerInputSource
    {
        private bool _jumpPressed;
        private bool _actionPressed;

        public float MoveX { get; set; }

        public bool JumpHeld { get; private set; }

        public void PressJump()
        {
            _jumpPressed = true;
            JumpHeld = true;
        }

        public void PressAction() => _actionPressed = true;

        public PlayerInput Sample()
        {
            var sample = new PlayerInput(MoveX, _jumpPressed, JumpHeld, _actionPressed);
            _jumpPressed = false;
            _actionPressed = false;
            return sample;
        }
    }
}
