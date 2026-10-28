using Godot;

namespace WarehouseRunner.Elements;

public enum TimeClockMode
{
    ClockIn,
    ClockOut,
}

[Tool]
[GlobalClass]
public partial class TimeClock : PlayerTrigger
{
    private static readonly Color Housing = new(0.82f, 0.82f, 0.78f);
    private static readonly Color HousingDark = new(0.35f, 0.35f, 0.33f);
    private static readonly Color BeamIdle = new(1f, 0.25f, 0.2f, 0.55f);
    private static readonly Color BeamDone = new(0.3f, 1f, 0.4f, 0.55f);
    private static readonly Color Rejected = new(1f, 0.2f, 0.2f);

    private TimeClockMode _mode = TimeClockMode.ClockIn;
    private bool _punched;
    private float _punchFlash;
    private float _rejectFlash;

    public TimeClock()
    {
        TriggerSize = new Vector2(24f, 900f);
    }

    [Export]
    public TimeClockMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            QueueRedraw();
        }
    }

    protected override Vector2 TriggerCenter => new(0f, -TriggerSize.Y / 2f);

    public void Punch()
    {
        _punched = true;
        _punchFlash = 1f;
    }

    public void Reject()
    {
        _rejectFlash = 1f;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _punchFlash = Mathf.Max(0f, _punchFlash - (dt * 1.5f));
        _rejectFlash = Mathf.Max(0f, _rejectFlash - (dt * 2.5f));
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawLightCurtain();
        DrawClock();
    }

    private void DrawLightCurtain()
    {
        var color = _punched ? BeamDone : BeamIdle;
        if (_rejectFlash > 0f)
        {
            color = color.Lerp(Rejected, _rejectFlash);
        }

        for (var y = 0f; y > -TriggerSize.Y; y -= 24f)
        {
            DrawLine(new Vector2(0f, y), new Vector2(0f, y - 12f), color, 3f);
        }
    }

    private void DrawClock()
    {
        var punchScale = 1f + (_punchFlash * 0.25f);
        DrawSetTransform(new Vector2(-34f, -130f), 0f, Vector2.One * punchScale);

        DrawRect(new Rect2(-24f, -36f, 48f, 72f), Housing);
        DrawRect(new Rect2(-24f, -36f, 48f, 72f), HousingDark, false, 3f);

        var face = new Vector2(0f, -12f);
        DrawCircle(face, 15f, Colors.White);
        DrawArc(face, 15f, 0f, Mathf.Tau, 24, HousingDark, 2f);
        var now = Time.GetTimeDictFromSystem();
        var minute = now["minute"].AsSingle();
        var hour = now["hour"].AsSingle() + (minute / 60f);
        DrawLine(face, face + (Vector2.Up.Rotated(hour / 12f * Mathf.Tau) * 8f), HousingDark, 2.5f);
        DrawLine(face, face + (Vector2.Up.Rotated(minute / 60f * Mathf.Tau) * 12f), HousingDark, 1.5f);

        DrawRect(new Rect2(-14f, 14f, 28f, 5f), HousingDark);
        var label = _mode == TimeClockMode.ClockIn ? "IN" : "OUT";
        DrawString(ThemeDB.FallbackFont, new Vector2(-14f, 33f), label, HorizontalAlignment.Left, -1, 12, HousingDark);

        if (_punchFlash > 0f)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(-40f, -50f), "KA-CHUNK!", HorizontalAlignment.Left, -1, 18, new Color(1f, 0.9f, 0.3f, _punchFlash));
        }

        DrawSetTransform(Vector2.Zero);
    }
}
