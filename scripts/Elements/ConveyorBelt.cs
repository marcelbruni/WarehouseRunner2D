using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class ConveyorBelt : BoxBody
{
    private const float ChevronSpacing = 36f;

    private static readonly Color Rubber = new(0.16f, 0.17f, 0.19f);
    private static readonly Color Frame = new(0.55f, 0.57f, 0.6f);
    private static readonly Color Roller = new(0.72f, 0.74f, 0.78f);
    private static readonly Color ChevronForward = new(0.3f, 0.9f, 0.4f);
    private static readonly Color ChevronBackward = new(1f, 0.45f, 0.2f);

    private float _beltSpeed = 200f;
    private float _scroll;

    public ConveyorBelt()
    {
        Size = new Vector2(400f, 24f);
    }

    [Export]
    public float BeltSpeed
    {
        get => _beltSpeed;
        set
        {
            _beltSpeed = value;
            ApplyBeltVelocity();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        ApplyBeltVelocity();
    }

    public override void _Process(double delta)
    {
        _scroll = Mathf.PosMod(_scroll + (_beltSpeed * (float)delta), ChevronSpacing);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = LocalRect;
        var radius = rect.Size.Y / 2f;
        DrawRect(rect, Frame);
        DrawRect(rect.Grow(-3f), Rubber);

        var color = _beltSpeed >= 0f ? ChevronForward : ChevronBackward;
        var direction = Mathf.Sign(_beltSpeed);
        var chevron = radius * 0.6f;
        for (var x = rect.Position.X + radius + _scroll; x < rect.End.X - radius; x += ChevronSpacing)
        {
            var tip = new Vector2(x + (direction * chevron * 0.5f), 0f);
            var tail = x - (direction * chevron * 0.5f);
            DrawPolyline([new Vector2(tail, -chevron), tip, new Vector2(tail, chevron)], color, 3f);
        }

        DrawCircle(new Vector2(rect.Position.X + radius, 0f), radius * 0.8f, Roller);
        DrawCircle(new Vector2(rect.End.X - radius, 0f), radius * 0.8f, Roller);
    }

    private void ApplyBeltVelocity()
    {
        ConstantLinearVelocity = Transform.X.Normalized() * _beltSpeed;
    }
}
