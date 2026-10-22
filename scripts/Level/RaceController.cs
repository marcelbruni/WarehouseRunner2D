using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Elements;
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
    private const string RecordsPath = "user://records.cfg";
    private const string RecordsSection = "best_times";

    private PlayerController _player = null!;
    private Hud _hud = null!;
    private Vector2 _respawnPosition;
    private double _elapsed;
    private double? _bestTime;

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
        _player = GetNode<PlayerController>("Player");
        _respawnPosition = _player.GlobalPosition;
        ApplyCameraBounds(_player.GetNode<Camera2D>("Camera"));

        _hud = new Hud();
        AddChild(_hud);
        _player.TrickPerformed += _hud.ShowTrick;

        ConnectTriggers(this);

        _bestTime = LoadBestTime();
        _hud.SetBest(_bestTime);
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
                _hud.ShowHint("Noch nicht Feierabend: Das Express-Paket muss erst in den LKW!");
                break;
        }
    }

    private void OnPackage(ExpressPackage package)
    {
        if (Phase == RunPhase.WaitingForClockIn)
        {
            _hud.ShowHint("Erst einstempeln!");
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
            _hud.ShowHint("Ohne Express-Paket fährt der LKW nicht los!");
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

    private void OnCheckpoint(Checkpoint checkpoint)
    {
        checkpoint.Activate();
        _respawnPosition = checkpoint.RespawnPosition;
    }

    private void Finish()
    {
        SetPhase(RunPhase.Finished);
        _hud.SetTime(_elapsed);
        _hud.ShowResult(_elapsed, _bestTime);
        if (SaveRecords && (_bestTime is null || _elapsed < _bestTime))
        {
            SaveBestTime(_elapsed);
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
            RunPhase.WaitingForClockIn => "Stempel dich ein!",
            RunPhase.SearchingPackage => "Finde das goldene Express-Paket",
            RunPhase.CarryingPackage => "Bring das Paket zur Rampe 3",
            RunPhase.PackageDelivered => "LKW ist weg, jetzt ausstempeln!",
            _ => "Feierabend!",
        });
    }

    private double? LoadBestTime()
    {
        var records = new ConfigFile();
        if (records.Load(RecordsPath) != Error.Ok || !records.HasSectionKey(RecordsSection, LevelId))
        {
            return null;
        }

        return records.GetValue(RecordsSection, LevelId).AsDouble();
    }

    private void SaveBestTime(double seconds)
    {
        var records = new ConfigFile();
        records.Load(RecordsPath);
        records.SetValue(RecordsSection, LevelId, seconds);
        records.Save(RecordsPath);
    }
}
