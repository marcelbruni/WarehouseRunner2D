using System.Linq;
using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Elements;
using WarehouseRunner.Menu;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

public enum RunPhase
{
    WaitingForClockIn,
    SearchingPackage,
    CarryingPackage,
    PackageDelivered,
    Finished,
}

public partial class RaceController : Node2D
{
    // Continue shares the gamepad A button with Jump, so a jump pressed while
    // clocking out must not skip the result screen.
    private const double ContinueLockSeconds = 0.4;

    private PlayerController _player = null!;
    private double _finishedFor;
    private Hud _hud = null!;
    private Vector2 _respawnPosition;
    private double _elapsed;
    private double? _bestTime;
    private MedalTimes? _medals;

    [Export]
    public string LevelId { get; set; } = "level01";

    [Export]
    public bool SaveRecords { get; set; } = true;

    [Export]
    public Rect2I CameraBounds { get; set; } = new(-1000, -1400, 9000, 1600);

    public RunPhase Phase { get; private set; } = RunPhase.WaitingForClockIn;

    public double ElapsedSeconds => _elapsed;

    public override void _Ready()
    {
        MusicPlayer.Play(MusicPlayer.LevelTrack(SceneFilePath));
        _player = GetNode<PlayerController>("Player");
        _respawnPosition = _player.GlobalPosition;
        ApplyCameraBounds(_player.GetNode<Camera2D>("Camera"));

        _hud = new Hud();
        AddChild(_hud);
        _player.TrickPerformed += _hud.ShowTrick;

        ConnectTriggers(this);

        _bestTime = RecordStore.LoadBestTime(LevelId);
        _medals = LevelCatalog.Find(LevelId)?.Medals;
        _hud.SetBest(_bestTime, _medals);
        _hud.SetTime(0);
        UpdateObjective();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Phase is not (RunPhase.WaitingForClockIn or RunPhase.Finished))
        {
            _elapsed += delta;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(InputSetup.Restart))
        {
            GetTree().ReloadCurrentScene();
            return;
        }

        if (Input.IsActionJustPressed(InputSetup.BackToMenu))
        {
            ReturnToMenu();
            return;
        }

        if (Phase == RunPhase.Finished)
        {
            _finishedFor += delta;
        }

        if (Phase == RunPhase.Finished && _finishedFor >= ContinueLockSeconds && Input.IsActionJustPressed(InputSetup.Continue))
        {
            if (LevelCatalog.NextAfter(LevelId) is { } next)
            {
                GetTree().ChangeSceneToFile(next.ScenePath);
            }
            else
            {
                ReturnToMenu();
            }

            return;
        }

        if (Input.IsActionJustPressed(InputSetup.ToggleHelp))
        {
            _hud.ToggleHelp();
        }

        _hud.SetTime(_elapsed);
        _hud.SetSpeed(Mathf.Abs(_player.Velocity.X), _player.Tuning.RunSpeed, _player.IsBoosted);
    }

    private void ApplyCameraBounds(Camera2D camera)
    {
        camera.LimitLeft = CameraBounds.Position.X;
        camera.LimitTop = CameraBounds.Position.Y;
        camera.LimitRight = CameraBounds.End.X;
        camera.LimitBottom = CameraBounds.End.Y;
    }

    private void ConnectTriggers(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            switch (child)
            {
                case TimeClock clock:
                    clock.PlayerEntered += _ => OnTimeClock(clock);
                    break;
                case ExpressPackage package:
                    package.PlayerEntered += _ => OnPackage(package);
                    break;
                case LoadingDock dock:
                    dock.PlayerEntered += _ => OnLoadingDock(dock);
                    break;
                case Checkpoint checkpoint:
                    checkpoint.PlayerEntered += _ => OnCheckpoint(checkpoint);
                    break;
                case KillZone killZone:
                    killZone.PlayerEntered += _ => _player.Respawn(_respawnPosition);
                    break;
                case ScanBeam scanBeam:
                    scanBeam.PlayerScanned += _ => OnScanned();
                    break;
            }

            ConnectTriggers(child);
        }
    }

    private void OnTimeClock(TimeClock clock)
    {
        switch (clock.Mode, Phase)
        {
            case (TimeClockMode.ClockIn, RunPhase.WaitingForClockIn):
                clock.Punch();
                SetPhase(RunPhase.SearchingPackage);
                break;
            case (TimeClockMode.ClockOut, RunPhase.PackageDelivered):
                clock.Punch();
                Finish();
                break;
            case (TimeClockMode.ClockOut, RunPhase.SearchingPackage or RunPhase.CarryingPackage):
                clock.Reject();
                _hud.ShowHint("Not done yet: the express package has to go on the truck first!");
                break;
        }
    }

    private void OnPackage(ExpressPackage package)
    {
        if (Phase == RunPhase.WaitingForClockIn)
        {
            _hud.ShowHint("Clock in first!");
            return;
        }

        if (Phase != RunPhase.SearchingPackage)
        {
            return;
        }

        package.Collect();
        _player.CarriesPackage = true;
        SetPhase(RunPhase.CarryingPackage);
    }

    private void OnLoadingDock(LoadingDock dock)
    {
        if (Phase == RunPhase.SearchingPackage)
        {
            _hud.ShowHint("The truck won't leave without the express package!");
            return;
        }

        if (Phase != RunPhase.CarryingPackage)
        {
            return;
        }

        _player.CarriesPackage = false;
        dock.DispatchTruck(_player.GlobalPosition + new Vector2(0f, -52f));
        SetPhase(RunPhase.PackageDelivered);
    }

    private void ReturnToMenu()
    {
        MainMenu.SelectLevel(LevelId);
        GetTree().ChangeSceneToFile(LevelCatalog.MenuScenePath);
    }

    private void OnScanned()
    {
        _player.Respawn(_respawnPosition);
        _hud.ShowHint("Scanned! Unknown item, back to the checkpoint.");
    }

    private void OnCheckpoint(Checkpoint checkpoint)
    {
        checkpoint.Activate();
        _respawnPosition = checkpoint.RespawnPosition;
    }

    private void Finish()
    {
        SetPhase(RunPhase.Finished);
        _hud.SetTime(_elapsed);
        var summary = string.Join("\n\n", GetChildren().OfType<IRunSummary>().Select(coach => coach.RunSummary()));
        _hud.ShowResult(_elapsed, _bestTime, hasNextLevel: LevelCatalog.NextAfter(LevelId) is not null, _medals, summary);
        if (SaveRecords && (_bestTime is null || _elapsed < _bestTime))
        {
            RecordStore.SaveBestTime(LevelId, _elapsed);
            _hud.SetBest(_elapsed, _medals);
        }
    }

    private void SetPhase(RunPhase phase)
    {
        Phase = phase;
        UpdateObjective();
    }

    private void UpdateObjective()
    {
        _hud.SetObjective(Phase switch
        {
            RunPhase.WaitingForClockIn => "Clock in!",
            RunPhase.SearchingPackage => "Find the golden express package",
            RunPhase.CarryingPackage => "Bring the package to dock 3",
            RunPhase.PackageDelivered => "Truck is gone, now clock out!",
            _ => "Shift over!",
        });
    }
}
