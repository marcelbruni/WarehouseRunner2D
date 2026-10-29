using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

// Speech bubble above the player that grades every wall kick: how long the player
// clung to the wall before kicking, and how much of the carried speed survived.
public partial class WallKickCoach : Node2D
{
    private const float ShowTime = 4.5f;
    private const float ScaleWidth = 130f;
    private const float RowHeight = 46f;
    private const float ClingScale = 0.3f;
    private static readonly Vector2 BubbleSize = new(176f, 104f);
    private static readonly Vector2 AbovePlayer = new(0f, -130f);
    private static readonly Color Perfect = new(0.35f, 0.9f, 0.4f);
    private static readonly Color Okay = new(1f, 0.65f, 0.15f);
    private static readonly Color Bad = new(0.95f, 0.3f, 0.3f);

    private PlayerController? _player;
    private float _timer;
    private float _clinging;
    private float _kept;
    private bool _chainBroken;

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 20;
        _player = GetParent().GetNodeOrNull<PlayerController>("Player");
        if (_player != null)
        {
            _player.WallKicked += OnWallKicked;
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

    private void OnWallKicked(float clinging, float kept, float speed, bool chainBroken)
    {
        _clinging = clinging;
        _kept = kept;
        _chainBroken = chainBroken;
        _timer = ShowTime;
    }

    private Color TimingColor()
    {
        var perfect = _player?.Tuning.WallKickPerfectTime ?? 0.05f;
        var lost = _player?.Tuning.WallKickSpeedLossTime ?? 0.25f;
        if (_clinging <= perfect)
        {
            return Perfect;
        }

        return _clinging < lost ? Okay : Bad;
    }

    public override void _Draw()
    {
        if (_timer <= 0f || _player == null)
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

        var timing = TimingColor() with { A = alpha };
        var perfectTime = _player.Tuning.WallKickPerfectTime;
        var clingText = _clinging <= perfectTime ? "Kick PERFECT!" : $"Kick +{_clinging * 1000f:0} ms at wall";
        DrawRow(bubble.Position.Y + 6f, clingText, timing, _clinging / ClingScale, 0f, perfectTime / ClingScale, alpha);

        var keptColor = _chainBroken ? Bad : timing;
        var keptText = _chainBroken ? "Reset by double jump" : $"Speed kept {_kept * 100f:0}%";
        DrawRow(bubble.Position.Y + 6f + RowHeight, keptText, keptColor with { A = alpha }, _kept, 0.95f, 1f, alpha);
    }

    // value, greenFrom and greenTo are positions on the scale from 0 to 1.
    private void DrawRow(float top, string text, Color color, float value, float greenFrom, float greenTo, float alpha)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(-BubbleSize.X / 2f, top + 18f), text, HorizontalAlignment.Center, BubbleSize.X, 17, color);
        var y = top + 32f;
        float ToX(float share) => -ScaleWidth / 2f + (Mathf.Clamp(share, 0f, 1f) * ScaleWidth);
        DrawLine(new Vector2(ToX(0f), y), new Vector2(ToX(1f), y), new Color(1f, 1f, 1f, 0.35f * alpha), 4f);
        DrawLine(new Vector2(ToX(greenFrom), y), new Vector2(ToX(greenTo), y), Perfect with { A = alpha }, 6f);
        DrawLine(new Vector2(ToX(value), y - 8f), new Vector2(ToX(value), y + 8f), color, 4f);
    }
}
