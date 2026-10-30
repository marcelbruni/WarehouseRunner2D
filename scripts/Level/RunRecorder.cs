using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

// Writes a frame-exact log of a run (inputs, state changes, tricks, rollout and wall kick timing)
// to user://runs so a run can be analysed afterwards.
public partial class RunRecorder : Node
{
    private const string Folder = "user://runs";
    private const int KeptRuns = 50;

    private readonly List<string> _lines = new() { "tick;race_s;event;x;y;vx;vy;state;detail" };
    private RaceController? _race;
    private PlayerController? _player;
    private PlayerState _lastState;
    private RunPhase _lastPhase;
    private long _tick;
    private bool _written;

    public override void _Ready()
    {
        _race = GetParent() as RaceController;
        _player = GetParent().GetNodeOrNull<PlayerController>("Player");
        if (_race == null || _player == null)
        {
            return;
        }

        _player.InputSource = new RecordingInput(_player.InputSource, this);
        _player.TrickPerformed += (trick, perfect) => Log("trick", $"{trick}{(perfect ? " perfect" : string.Empty)}");
        _player.Landed += impact => Log("landed", $"impact {impact:0}");
        _player.RolloutTimed += offset => Log("rollout_timing", float.IsNaN(offset) ? "missed" : $"{offset * 1000f:0} ms");
        _player.WallKicked += (contactAge, kept, horizontal, chainBroken) =>
            Log("wall_kick", $"{contactAge * 1000f:0} ms at wall, kept {kept * 100f:0}%, horizontal {horizontal:0}{(chainBroken ? ", chain broken" : string.Empty)}");
        _lastState = _player.State;
        _lastPhase = _race.Phase;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_race == null || _player == null || _written)
        {
            return;
        }

        _tick++;
        if (_player.State != _lastState)
        {
            Log("state", $"{_lastState} -> {_player.State}");
            _lastState = _player.State;
        }

        if (_race.Phase != _lastPhase)
        {
            Log("phase", _race.Phase.ToString());
            _lastPhase = _race.Phase;
            if (_race.Phase == RunPhase.Finished)
            {
                Write("finished");
            }
        }
    }

    public override void _ExitTree()
    {
        if (_race is { Phase: not RunPhase.WaitingForClockIn })
        {
            Write("aborted");
        }
    }

    private void Log(string kind, string detail)
    {
        if (_player == null || _race == null)
        {
            return;
        }

        var p = _player.GlobalPosition;
        var v = _player.Velocity;
        _lines.Add(string.Join(';',
            _tick.ToString(CultureInfo.InvariantCulture),
            _race.ElapsedSeconds.ToString("0.000", CultureInfo.InvariantCulture),
            kind,
            p.X.ToString("0", CultureInfo.InvariantCulture),
            p.Y.ToString("0", CultureInfo.InvariantCulture),
            v.X.ToString("0", CultureInfo.InvariantCulture),
            v.Y.ToString("0", CultureInfo.InvariantCulture),
            _player.State.ToString(),
            detail));
    }

    private void Write(string outcome)
    {
        if (_written || _race == null || _player?.InputSource is not RecordingInput)
        {
            return;
        }

        _written = true;
        DirAccess.MakeDirRecursiveAbsolute(Folder);
        var stamp = Time.GetDatetimeStringFromSystem().Replace(':', '-').Replace('T', '_');
        using var file = FileAccess.Open($"{Folder}/{_race.LevelId}_{stamp}_{outcome}.csv", FileAccess.ModeFlags.Write);
        foreach (var line in _lines)
        {
            file.StoreLine(line);
        }

        DeleteOldestRuns();
    }

    private static void DeleteOldestRuns()
    {
        var runs = DirAccess.GetFilesAt(Folder).Where(name => name.EndsWith(".csv")).ToList();
        var stale = runs.Count - KeptRuns;
        if (stale <= 0)
        {
            return;
        }

        foreach (var name in runs.OrderBy(name => FileAccess.GetModifiedTime($"{Folder}/{name}")).Take(stale))
        {
            DirAccess.RemoveAbsolute($"{Folder}/{name}");
        }
    }

    private sealed class RecordingInput(IPlayerInputSource inner, RunRecorder recorder) : IPlayerInputSource
    {
        private PlayerInput _last;

        public PlayerInput Sample()
        {
            var input = inner.Sample();
            if (input.JumpPressed)
            {
                recorder.Log("press", "jump");
            }

            if (input.ActionPressed)
            {
                recorder.Log("press", "action");
            }

            if (input.DashPressed)
            {
                recorder.Log("press", "dash");
            }

            if (!Mathf.IsEqualApprox(input.MoveX, _last.MoveX) || input.CrouchHeld != _last.CrouchHeld || !Mathf.IsEqualApprox(input.AimY, _last.AimY))
            {
                recorder.Log("hold", $"move {input.MoveX:0.##} aim {input.AimY:0.##} crouch {input.CrouchHeld}");
            }

            _last = input;
            return input;
        }
    }
}
