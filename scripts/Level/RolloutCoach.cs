using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

// Speech bubble above the player that grades every rollout: text plus a small timing
// scale with the landing in the middle and the perfect window marked green.
public partial class RolloutCoach : Node2D
{
    private const float ShowTime = 0.9f;
    private const float ScaleEarly = -0.15f;
    private const float ScaleLate = 0.12f;
    private const float ScaleWidth = 130f;
    private static readonly Vector2 BubbleSize = new(164f, 62f);
    private static readonly Vector2 AbovePlayer = new(0f, -110f);
    private static readonly Color Perfect = new(0.35f, 0.9f, 0.4f);
    private static readonly Color Okay = new(1f, 0.65f, 0.15f);
    private static readonly Color Missed = new(0.95f, 0.3f, 0.3f);

    private PlayerController? _player;
    private float _offset;
    private float _timer;

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = 20;
        _player = GetParent().GetNodeOrNull<PlayerController>("Player");
        if (_player != null)
        {
            _player.RolloutTimed += OnRolloutTimed;
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

    private void OnRolloutTimed(float offset)
    {
        _offset = offset;
        _timer = ShowTime;
    }

    public override void _Draw()
    {
        if (_timer <= 0f || _player == null)
        {
            return;
        }

        var alpha = Mathf.Clamp(_timer / 0.25f, 0f, 1f);
        var missed = float.IsNaN(_offset);
        var perfect = !missed && Mathf.Abs(_offset) <= _player.Tuning.PerfectRollWindow;
        var color = missed ? Missed : perfect ? Perfect : Okay;
        var text = missed ? "MISSED" : perfect ? "PERFECT!" : $"{(_offset < 0f ? "early" : "late")} {Mathf.Abs(_offset) * 1000f:0} ms";

        var bubble = new Rect2(-BubbleSize / 2f, BubbleSize);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.11f, 0.85f * alpha),
            BorderColor = color with { A = alpha },
            BorderWidthBottom = 3, BorderWidthTop = 3, BorderWidthLeft = 3, BorderWidthRight = 3,
            CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10,
        };
        style.Draw(GetCanvasItem(), bubble);
        DrawColoredPolygon([new Vector2(-9f, bubble.End.Y - 1f), new Vector2(9f, bubble.End.Y - 1f), new Vector2(0f, bubble.End.Y + 12f)], color with { A = alpha });

        var font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(bubble.Position.X, bubble.Position.Y + 26f), text, HorizontalAlignment.Center, BubbleSize.X, 20, color with { A = alpha });

        var y = bubble.Position.Y + 44f;
        float ToX(float seconds) => -ScaleWidth / 2f + ((seconds - ScaleEarly) / (ScaleLate - ScaleEarly) * ScaleWidth);
        var window = _player.Tuning.PerfectRollWindow;
        DrawLine(new Vector2(ToX(ScaleEarly), y), new Vector2(ToX(ScaleLate), y), new Color(1f, 1f, 1f, 0.35f * alpha), 4f);
        DrawLine(new Vector2(ToX(-window), y), new Vector2(ToX(window), y), Perfect with { A = alpha }, 6f);
        DrawLine(new Vector2(ToX(0f), y - 6f), new Vector2(ToX(0f), y + 6f), new Color(1f, 1f, 1f, alpha), 2f);
        if (!missed)
        {
            var x = ToX(Mathf.Clamp(_offset, ScaleEarly, ScaleLate));
            DrawLine(new Vector2(x, y - 9f), new Vector2(x, y + 9f), color with { A = alpha }, 4f);
        }
    }
}
