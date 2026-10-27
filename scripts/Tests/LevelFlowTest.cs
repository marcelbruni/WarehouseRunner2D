using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Elements;
using WarehouseRunner.Level;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class LevelFlowTest : Node
{
    private const string SnapshotArgument = "--snapshots=";
    private static readonly Vector2 StandOffset = new(0f, -26f);

    private readonly List<string> _failures = new();
    private string? _snapshotDirectory;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _snapshotDirectory = OS.GetCmdlineUserArgs()
            .FirstOrDefault(a => a.StartsWith(SnapshotArgument))?[SnapshotArgument.Length..];

        CheckMedalTimes();
        foreach (var level in LevelCatalog.Levels)
        {
            await RunLevel(level);
        }

        foreach (var failure in _failures)
        {
            GD.PrintErr(failure);
        }

        GD.Print(_failures.Count == 0 ? "PASS LevelFlow" : "FAIL LevelFlow");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private void CheckMedalTimes()
    {
        foreach (var level in LevelCatalog.Levels)
        {
            var m = level.Medals;
            if (!(m.Gold > 0 && m.Gold < m.Silver && m.Silver < m.Bronze))
            {
                _failures.Add($"{level.Id}: medal times out of order (gold {m.Gold}, silver {m.Silver}, bronze {m.Bronze})");
            }
        }

        var medals = new MedalTimes(7.0, 8.0, 9.5);
        var cases = new (double Time, Medal Expected)[]
        {
            (6.5, Medal.Gold), (7.004, Medal.Gold), (7.006, Medal.Silver), (8.0, Medal.Silver),
            (9.5, Medal.Bronze), (9.51, Medal.None),
        };
        foreach (var (time, expected) in cases)
        {
            if (medals.For(time) != expected)
            {
                _failures.Add($"medal for {time}: {medals.For(time)}, expected {expected}");
            }
        }
    }

    private async Task RunLevel(LevelInfo level)
    {
        var race = GD.Load<PackedScene>(level.ScenePath).Instantiate<RaceController>();
        race.ProcessMode = ProcessModeEnum.Pausable;
        race.SaveRecords = false;
        if (race.LevelId != level.Id)
        {
            _failures.Add($"{level.Id}: scene saves records as '{race.LevelId}'");
        }

        if (!ResourceLoader.Exists(MusicPlayer.LevelTrack(level.ScenePath)))
        {
            _failures.Add($"{level.Id}: no music track {MusicPlayer.LevelTrack(level.ScenePath)}");
        }

        AddChild(race);
        var player = race.GetNode<PlayerController>("Player");
        player.InputSource = new IdleInput();

        var clocks = FindAll<TimeClock>(race);
        var clockIn = clocks.Single(c => c.Mode == TimeClockMode.ClockIn);
        var clockOut = clocks.Single(c => c.Mode == TimeClockMode.ClockOut);
        var package = FindAll<ExpressPackage>(race).Single();
        var dock = FindAll<LoadingDock>(race).Single();

        await Ticks(30);
        await Snapshot($"{level.Id}_01_start");
        Expect(level, race, "start", RunPhase.WaitingForClockIn);

        await Visit(player, package.GlobalPosition);
        Expect(level, race, "package before clock in", RunPhase.WaitingForClockIn);

        await Visit(player, clockIn.GlobalPosition + StandOffset);
        Expect(level, race, "after clock in", RunPhase.SearchingPackage);

        await Visit(player, dock.GlobalPosition + StandOffset);
        Expect(level, race, "dock without package", RunPhase.SearchingPackage);

        for (var attempt = 0; attempt < 6 && race.Phase != RunPhase.CarryingPackage; attempt++)
        {
            await Visit(player, package.GlobalPosition);
            await Ticks(60);
        }

        Expect(level, race, "after package", RunPhase.CarryingPackage);
        await Snapshot($"{level.Id}_02_package");

        await Visit(player, clockOut.GlobalPosition + StandOffset);
        Expect(level, race, "clock out with package", RunPhase.CarryingPackage);

        await Visit(player, dock.GlobalPosition + StandOffset);
        Expect(level, race, "after delivery", RunPhase.PackageDelivered);

        await Visit(player, clockOut.GlobalPosition + StandOffset);
        Expect(level, race, "after clock out", RunPhase.Finished);
        await Ticks(30);
        await Snapshot($"{level.Id}_03_finished");

        RemoveChild(race);
        race.QueueFree();
    }

    private async Task Visit(PlayerController player, Vector2 position)
    {
        player.Respawn(position);
        await Ticks(5);
    }

    private void Expect(LevelInfo level, RaceController race, string step, RunPhase expected)
    {
        if (race.Phase != expected)
        {
            _failures.Add($"{level.Id} {step}: phase {race.Phase}, expected {expected}");
        }
    }

    private static List<T> FindAll<T>(Node root)
        where T : Node
    {
        var found = new List<T>();
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
            {
                found.Add(match);
            }

            found.AddRange(FindAll<T>(child));
        }

        return found;
    }

    private async Task Snapshot(string name)
    {
        if (_snapshotDirectory is null)
        {
            return;
        }

        GetTree().Paused = true;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().GetTexture().GetImage().SavePng($"{_snapshotDirectory}/{name}.png");
        GetTree().Paused = false;
    }

    private async Task Ticks(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    private sealed class IdleInput : IPlayerInputSource
    {
        public PlayerInput Sample() => default;
    }
}
