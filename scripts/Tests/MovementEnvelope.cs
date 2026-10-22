using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Elements;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class MovementEnvelope : Node2D
{
    private const int LanesPerRow = 25;
    private static readonly Vector2 LaneSpacing = new(8000f, 1500f);
    private const int RunUpTicks = 60;
    private const int FlightTicks = 260;

    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");

    private enum Opener
    {
        RunningJump,
        SlideJump,
        RollJump,
    }

    private sealed record Technique(string Name, Opener Opener, int? DoubleJumpTick, int? DashTick);

    private sealed class Probe
    {
        public required Technique Technique { get; init; }
        public required PlayerController Player { get; init; }
        public required ConcreteBlock Floor { get; init; }
        public required ScriptedInput Input { get; init; }
        public float TakeoffX { get; set; }
        public float TakeoffY { get; set; }
        public int TakeoffTick { get; set; } = -1;
        public List<Vector2> Path { get; } = new();
    }

    public override async void _Ready()
    {
        var techniques = BuildTechniques();
        var probes = new List<Probe>();
        for (var i = 0; i < techniques.Count; i++)
        {
            probes.Add(CreateProbe(techniques[i], new Vector2(i % LanesPerRow, i / LanesPerRow) * LaneSpacing));
        }

        await RunProbes(probes);
        Report(probes);
        GetTree().Quit();
    }

    private static List<Technique> BuildTechniques()
    {
        var list = new List<Technique>();
        foreach (var opener in Enum.GetValues<Opener>())
        {
            list.Add(new Technique("jump", opener, null, null));
            for (var dj = 4; dj <= 80; dj += 4)
            {
                list.Add(new Technique("jump+dj", opener, dj, null));
            }

            for (var dash = 4; dash <= 80; dash += 4)
            {
                list.Add(new Technique("jump+dash", opener, null, dash));
            }

            for (var dj = 8; dj <= 72; dj += 8)
            {
                for (var dash = dj + 4; dash <= dj + 60; dash += 8)
                {
                    list.Add(new Technique("jump+dj+dash", opener, dj, dash));
                }
            }

            for (var dash = 8; dash <= 72; dash += 8)
            {
                for (var dj = dash + 2; dj <= dash + 40; dj += 4)
                {
                    list.Add(new Technique("jump+dash+dj", opener, dj, dash));
                }
            }
        }

        return list;
    }

    private Probe CreateProbe(Technique technique, Vector2 lane)
    {
        var floor = new ConcreteBlock { Position = lane + new Vector2(0f, 100f), Size = new Vector2(6000f, 200f) };
        AddChild(floor);
        var input = new ScriptedInput();
        var player = PlayerScene.Instantiate<PlayerController>();
        player.InputSource = input;
        var startX = technique.Opener == Opener.SlideJump ? 600f : -900f;
        player.Position = lane + new Vector2(startX, -26f);
        AddChild(player);
        return new Probe { Technique = technique, Player = player, Floor = floor, Input = input };
    }

    private async Task RunProbes(List<Probe> probes)
    {
        for (var tick = 0; tick < 10; tick++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        var total = RunUpTicks + 20 + FlightTicks;
        for (var tick = 0; tick < total; tick++)
        {
            foreach (var probe in probes)
            {
                Drive(probe, tick);
            }

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            foreach (var probe in probes.Where(p => p.TakeoffTick >= 0))
            {
                probe.Path.Add(probe.Player.GlobalPosition);
            }
        }
    }

    private static void Drive(Probe probe, int tick)
    {
        var input = probe.Input;
        var technique = probe.Technique;
        if (probe.TakeoffTick < 0)
        {
            DriveRunUp(probe, tick);
            if (probe.Player.State == PlayerState.Airborne && tick > RunUpTicks / 2)
            {
                probe.TakeoffTick = tick;
                probe.TakeoffX = probe.Player.GlobalPosition.X;
                probe.TakeoffY = probe.Player.GlobalPosition.Y;
                probe.Floor.CollisionLayer = 0;
                probe.Player.CollisionMask = 0;
            }

            return;
        }

        var flightTick = tick - probe.TakeoffTick;
        input.MoveX = 1f;
        if (flightTick == technique.DoubleJumpTick)
        {
            input.PressJump();
        }

        if (flightTick == technique.DashTick)
        {
            input.PressAction();
        }
    }

    private static void DriveRunUp(Probe probe, int tick)
    {
        var input = probe.Input;
        switch (probe.Technique.Opener)
        {
            case Opener.RunningJump:
                input.MoveX = 1f;
                if (tick == RunUpTicks)
                {
                    input.PressJump();
                }

                break;
            case Opener.SlideJump:
                input.MoveX = tick < RunUpTicks ? -1f : 1f;
                if (tick == RunUpTicks + 3)
                {
                    input.PressJump();
                }

                break;
            case Opener.RollJump:
                input.MoveX = 1f;
                if (tick == RunUpTicks)
                {
                    input.PressAction();
                }

                if (tick == RunUpTicks + 3)
                {
                    input.PressJump();
                }

                break;
        }
    }

    private static void Report(List<Probe> probes)
    {
        GD.Print("opener       | technique     | apex | range@0 | range@-100(higher) | range@+150(lower)");
        foreach (var group in probes.GroupBy(p => (p.Technique.Opener, p.Technique.Name)))
        {
            var apex = group.Max(p => Apex(p));
            var level = group.Max(p => RangeAtHeight(p, 0f));
            var higher = group.Max(p => RangeAtHeight(p, 100f));
            var lower = group.Max(p => RangeAtHeight(p, -150f));
            GD.Print($"{group.Key.Opener,-12} | {group.Key.Name,-13} | {apex,4:0} | {level,7:0} | {higher,18:0} | {lower,17:0}");
        }
    }

    private static float Apex(Probe probe) =>
        probe.Path.Count == 0 ? 0f : probe.TakeoffY - probe.Path.Min(p => p.Y);

    private static float RangeAtHeight(Probe probe, float heightAboveTakeoff)
    {
        var targetY = probe.TakeoffY - heightAboveTakeoff;
        var best = 0f;
        for (var i = 1; i < probe.Path.Count; i++)
        {
            var descending = probe.Path[i].Y > probe.Path[i - 1].Y;
            if (descending && probe.Path[i - 1].Y <= targetY && probe.Path[i].Y >= targetY)
            {
                best = Mathf.Max(best, probe.Path[i].X - probe.TakeoffX);
            }
        }

        return best;
    }

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
