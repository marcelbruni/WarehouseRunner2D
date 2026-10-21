using Godot;

namespace WarehouseRunner.Player;

public partial class CameraRig : Camera2D
{
    [Export]
    public float LookAhead { get; set; } = 0.3f;

    [Export]
    public float MaxLookAhead { get; set; } = 220f;

    [Export]
    public float VerticalBias { get; set; } = -60f;

    private PlayerController _player = null!;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
    }

    public override void _Process(double delta)
    {
        var lookAhead = Mathf.Clamp(_player.Velocity.X * LookAhead, -MaxLookAhead, MaxLookAhead);
        var target = new Vector2(lookAhead, VerticalBias);
        Offset = Offset.Lerp(target, Mathf.Min(1f, (float)delta * 3f));
    }
}
