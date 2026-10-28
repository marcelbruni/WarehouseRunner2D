using Godot;

namespace WarehouseRunner.Level;

public partial class SpeedMeter : Control
{
    private const float FullScaleFactor = 2.2f;

    private static readonly Color Background = new(0f, 0f, 0f, 0.5f);
    private static readonly Color Normal = new(0.35f, 0.8f, 1f);
    private static readonly Color Boosted = new(1f, 0.55f, 0.15f);

    private float _speed;
    private float _runSpeed = 1f;
    private bool _boosted;

    public void Update(float speed, float runSpeed, bool boosted)
    {
        _speed = speed;
        _runSpeed = runSpeed;
        _boosted = boosted;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var bar = new Rect2(Vector2.Zero, Size);
        DrawRect(bar, Background);
        var fill = Mathf.Clamp(_speed / (_runSpeed * FullScaleFactor), 0f, 1f);
        DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * fill, bar.Size.Y)), _boosted ? Boosted : Normal);
        var runMark = bar.Size.X / FullScaleFactor;
        DrawLine(new Vector2(runMark, 0f), new Vector2(runMark, bar.Size.Y), Colors.White, 2f);
        DrawString(ThemeDB.FallbackFont, new Vector2(6f, bar.Size.Y - 8f), _boosted ? "BOOST" : "SPEED", HorizontalAlignment.Left, -1, 14, Colors.White);
    }
}
