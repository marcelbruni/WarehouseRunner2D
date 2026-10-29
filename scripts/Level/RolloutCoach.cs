using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

// Speech bubble above the player that grades the two timings of the speed rotation,
// one row each: how soon the dash follows the jump, and how close the rollout press is
// to the landing. A row from the previous rotation is dimmed until it is replaced.
public partial class RolloutCoach : Node2D, IRunSummary
{
    private const float ShowTime = 1.5f;
    private const float ScaleWidth = 130f;
    private const float DashGood = 0.035f;
    private const float DashOkay = 0.07f;
    private const float RowHeight = 46f;
    private static readonly Vector2 BubbleSize = new(176f, 104f);
    private static readonly Vector2 AbovePlayer = new(0f, -130f);
    private static readonly Color Perfect = new(0.35f, 0.9f, 0.4f);
    private static readonly Color Okay = new(1f, 0.65f, 0.15f);
    private static readonly Color Missed = new(0.95f, 0.3f, 0.3f);

    private readonly record struct Reading(string Text, Color Color, float ScaleMin, float ScaleMax, float GreenMin, float GreenMax, float? Mark, float? Reference, ulong Frame);

    private PlayerController? _player;
    private RaceController? _race;
    private Reading? _dash;
    private Reading? _rollout;
    private float _timer;
    private ulong? _jumpFrame;
    private int _dashes;
    private float _dashDelayTotal;
    private int _rollouts;
    private int _perfectRollouts;
    private int _missedRollouts;

    private bool IsRunning => _race is { Phase: not (RunPhase.WaitingForClockIn or RunPhase.Finished) };

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 20;
        _race = GetParent() as RaceController;
        _player = GetParent().GetNodeOrNull<PlayerController>("Player");
        if (_player != null)
        {
            _player.RolloutTimed += OnRolloutTimed;
            _player.TrickPerformed += OnTrick;
            _player.Landed += _ => _jumpFrame = null;
        }
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_player != null)
        {
            GlobalPosition = _player.GlobalPosition + AbovePlayer;
        }

        QueueRedraw();
    }

    private void OnTrick(Trick trick, bool perfect)
    {
        if (trick is Trick.Jump or Trick.RollJump or Trick.SlideJump)
        {
            _jumpFrame = Engine.GetPhysicsFrames();
            return;
        }

        if (trick is not (Trick.Dash or Trick.PowerDash) || _jumpFrame is not { } jumped)
        {
            return;
        }

        _jumpFrame = null;
        var now = Engine.GetPhysicsFrames();
        var delay = (now - jumped) / (float)Engine.PhysicsTicksPerSecond;
        _dash = new Reading($"Dash +{delay * 1000f:0} ms", DashColor(delay), 0f, 0.2f, 0f, DashGood, delay, null, now);
        _timer = ShowTime;
        if (IsRunning)
        {
            _dashes++;
            _dashDelayTotal += delay;
        }
    }

    private void OnRolloutTimed(float offset)
    {
        var window = _player?.Tuning.PerfectRollWindow ?? 0.05f;
        var now = Engine.GetPhysicsFrames();
        if (IsRunning)
        {
            _rollouts++;
            _missedRollouts += float.IsNaN(offset) ? 1 : 0;
            _perfectRollouts += Mathf.Abs(offset) <= window ? 1 : 0;
        }

        if (float.IsNaN(offset))
        {
            _rollout = new Reading("Rollout MISSED", Missed, -0.15f, 0.12f, -window, window, null, 0f, now);
        }
        else
        {
            var perfect = Mathf.Abs(offset) <= window;
            var direction = offset < 0f ? "early" : "late";
            var text = perfect ? "Rollout PERFECT!" : $"Rollout {direction} {Mathf.Abs(offset) * 1000f:0} ms";
            _rollout = new Reading(text, perfect ? Perfect : Okay, -0.15f, 0.12f, -window, window, offset, 0f, now);
        }

        _timer = ShowTime;
    }

    public string RunSummary()
    {
        var dashLine = _dashes == 0 ? "Dashes after jump: none" : $"Dashes after jump: {_dashes}\nAvg dash delay: {_dashDelayTotal / _dashes * 1000f:0} ms";
        var rolloutLine = _rollouts == 0 ? "Rollouts: none" : $"Rollouts perfect: {_perfectRollouts} of {_rollouts}\nRollouts missed: {_missedRollouts}";
        return $"{dashLine}\n{rolloutLine}";
    }

    private static Color DashColor(float delay) => delay switch
    {
        <= DashGood => Perfect,
        <= DashOkay => Okay,
        _ => Missed,
    };

    public override void _Draw()
    {
        if (_timer <= 0f || (_dash is null && _rollout is null))
        {
            return;
        }

        var alpha = Mathf.Clamp(_timer / 0.25f, 0f, 1f);
        var bubble = new Rect2(-BubbleSize / 2f, BubbleSize);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.11f, 0.85f * alpha),
            BorderColor = new Color(1f, 1f, 1f, 0.6f * alpha),
            BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
        };
        style.Draw(GetCanvasItem(), bubble);
        DrawColoredPolygon([new Vector2(-9f, bubble.End.Y - 1f), new Vector2(9f, bubble.End.Y - 1f), new Vector2(0f, bubble.End.Y + 12f)], new Color(1f, 1f, 1f, 0.6f * alpha));

        var rolloutIsStale = _rollout is { } rollout && _dash is { } dash && rollout.Frame < dash.Frame;
        DrawRow(_dash, bubble.Position.Y + 6f, alpha);
        DrawRow(_rollout, bubble.Position.Y + 6f + RowHeight, rolloutIsStale ? alpha * 0.4f : alpha);
    }

    private void DrawRow(Reading? reading, float top, float alpha)
    {
        if (reading is not { } r)
        {
            return;
        }

        var color = r.Color with { A = alpha };
        DrawString(ThemeDB.FallbackFont, new Vector2(-BubbleSize.X / 2f, top + 18f), r.Text, HorizontalAlignment.Center, BubbleSize.X, 17, color);

        var y = top + 32f;
        float ToX(float seconds) => -ScaleWidth / 2f + ((Mathf.Clamp(seconds, r.ScaleMin, r.ScaleMax) - r.ScaleMin) / (r.ScaleMax - r.ScaleMin) * ScaleWidth);
        DrawLine(new Vector2(ToX(r.ScaleMin), y), new Vector2(ToX(r.ScaleMax), y), new Color(1f, 1f, 1f, 0.35f * alpha), 4f);
        DrawLine(new Vector2(ToX(r.GreenMin), y), new Vector2(ToX(r.GreenMax), y), Perfect with { A = alpha }, 6f);
        if (r.Reference is { } reference)
        {
            DrawLine(new Vector2(ToX(reference), y - 6f), new Vector2(ToX(reference), y + 6f), new Color(1f, 1f, 1f, alpha), 2f);
        }

        if (r.Mark is { } mark)
        {
            DrawLine(new Vector2(ToX(mark), y - 8f), new Vector2(ToX(mark), y + 8f), color, 4f);
        }
    }
}
