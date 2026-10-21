using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class Checkpoint : PlayerTrigger
{
    private static readonly Color Pole = new(0.55f, 0.57f, 0.6f);
    private static readonly Color Inactive = new(0.6f, 0.25f, 0.2f);
    private static readonly Color Active = new(0.3f, 0.85f, 0.35f);

    private bool _isActive;

    public Checkpoint()
    {
        TriggerSize = new Vector2(48f, 600f);
    }

    public Vector2 RespawnPosition => GlobalPosition + new Vector2(0f, -40f);

    protected override Vector2 TriggerCenter => new(0f, -TriggerSize.Y / 2f);

    public void Activate()
    {
        _isActive = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawLine(Vector2.Zero, new Vector2(0f, -110f), Pole, 4f);
        DrawRect(new Rect2(-26f, -134f, 52f, 26f), _isActive ? Active : Inactive);
        DrawString(ThemeDB.FallbackFont, new Vector2(-22f, -115f), _isActive ? "OK" : "CHECK", HorizontalAlignment.Left, -1, 12);
    }
}
