using System;
using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class ScanBeam : PlayerTrigger
{
    private const float WarningTime = 0.35f;

    private static readonly Color Housing = new(0.2f, 0.21f, 0.24f);
    private static readonly Color BeamOn = new(1f, 0.15f, 0.15f, 0.75f);
    private static readonly Color BeamWarn = new(1f, 0.6f, 0.2f, 0.35f);
    private static readonly Color BeamOff = new(0.3f, 1f, 0.4f, 0.25f);

    private float _time;

    public ScanBeam()
    {
        TriggerSize = new Vector2(16f, 300f);
    }

    public event Action<PlayerController>? PlayerScanned;

    [Export]
    public float OnTime { get; set; } = 0.9f;

    [Export]
    public float OffTime { get; set; } = 1.1f;

    [Export(PropertyHint.Range, "0,1")]
    public float Phase { get; set; }

    public bool IsActive => CycleTime >= OffTime;

    private float CycleTime => Mathf.PosMod(_time + (Phase * (OnTime + OffTime)), OnTime + OffTime);

    protected override Vector2 TriggerCenter => new(0f, -TriggerSize.Y / 2f);

    public override void _PhysicsProcess(double delta)
    {
        _time += (float)delta;
        if (Engine.IsEditorHint() || !IsActive)
        {
            return;
        }

        foreach (var body in GetOverlappingBodies())
        {
            if (body is PlayerController player)
            {
                PlayerScanned?.Invoke(player);
            }
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var height = TriggerSize.Y;
        var warning = !IsActive && CycleTime > OffTime - WarningTime;
        var color = IsActive ? BeamOn : warning ? BeamWarn : BeamOff;
        var width = IsActive ? 6f + (Mathf.Sin(_time * 40f) * 2f) : 2f;
        DrawLine(Vector2.Zero, new Vector2(0f, -height), color, width);
        if (IsActive)
        {
            for (var y = -20f; y > -height; y -= 46f)
            {
                DrawLine(new Vector2(-8f, y), new Vector2(8f, y), BeamOn, 2f);
            }
        }

        DrawRect(new Rect2(-14f, -height - 16f, 28f, 16f), Housing);
        DrawRect(new Rect2(-14f, -6f, 28f, 6f), Housing);
        DrawString(ThemeDB.FallbackFont, new Vector2(-22f, -height - 22f), "SCAN", HorizontalAlignment.Left, -1, 14, color with { A = 1f });
    }
}
