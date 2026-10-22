using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Level;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class LevelFlowTest : Node
{
    private const string SnapshotArgument = "--snapshots=";

    private readonly List<string> _failures = new();
    private RaceController _race = null!;
    private PlayerController _player = null!;
    private string? _snapshotDirectory;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(SnapshotArgument))
            {
                _snapshotDirectory = argument[SnapshotArgument.Length..];
            }
        }

        _race = GD.Load<PackedScene>("res://scenes/levels/Level01.tscn").Instantiate<RaceController>();
        _race.ProcessMode = ProcessModeEnum.Pausable;
        _race.SaveRecords = false;
        AddChild(_race);
        _player = _race.GetNode<PlayerController>("Player");
        _player.InputSource = new IdleInput();

        await Ticks(60);
        await Snapshot("01_start");
        ExpectPhase("start", RunPhase.WaitingForClockIn);

        await VisitPackageBeforeClockIn();

        await TeleportAndWait(new Vector2(-400f, -26f));
        ExpectPhase("after clock in", RunPhase.SearchingPackage);
        await Snapshot("02_clock_in");

        await SnapshotAt(new Vector2(600f, -26f), "03_first_slope");
        await SnapshotAt(new Vector2(2300f, -300f), "04_crate_wall");
        await SnapshotAt(new Vector2(3150f, -40f), "05_fast_belt");
        await SnapshotAt(new Vector2(4250f, -160f), "06_escalator");
        await SnapshotAt(new Vector2(5300f, -400f), "07_racks");

        await TeleportAndWait(new Vector2(6480f, -26f));
        ExpectPhase("dock without package", RunPhase.SearchingPackage);

        await TeleportAndWait(new Vector2(5180f, -730f));
        ExpectPhase("after package", RunPhase.CarryingPackage);
        await Snapshot("08_package");

        await TeleportAndWait(new Vector2(6800f, -26f));
        ExpectPhase("clock out with package", RunPhase.CarryingPackage);

        await TeleportAndWait(new Vector2(6480f, -26f));
        ExpectPhase("after delivery", RunPhase.PackageDelivered);
        await Ticks(60);
        await Snapshot("09_truck_loading");
        await Ticks(240);
        await Snapshot("10_truck_gone");

        await TeleportAndWait(new Vector2(6800f, -26f));
        ExpectPhase("after clock out", RunPhase.Finished);
        if (_race.ElapsedSeconds is < 4.5 or > 7.0)
        {
            _failures.Add($"race time {_race.ElapsedSeconds:0.00}s, expected the scripted ~5.4s");
        }

        await Ticks(30);
        await Snapshot("11_finished");

        foreach (var failure in _failures)
        {
            GD.PrintErr(failure);
        }

        GD.Print(_failures.Count == 0 ? "PASS LevelFlow" : "FAIL LevelFlow");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private async Task VisitPackageBeforeClockIn()
    {
        await TeleportAndWait(new Vector2(5180f, -730f));
        ExpectPhase("package before clock in", RunPhase.WaitingForClockIn);
        await TeleportAndWait(new Vector2(-650f, -26f));
    }

    private async Task SnapshotAt(Vector2 position, string name)
    {
        await TeleportAndWait(position);
        await Ticks(60);
        await Snapshot(name);
    }

    private async Task TeleportAndWait(Vector2 position)
    {
        _player.Respawn(position);
        await Ticks(5);
    }

    private void ExpectPhase(string step, RunPhase expected)
    {
        if (_race.Phase != expected)
        {
            _failures.Add($"{step}: phase {_race.Phase}, expected {expected}");
        }
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
