using Godot;

namespace WarehouseRunner.Elements;

[Tool]
public partial class BoxBody : StaticBody2D
{
    private Vector2 _size = new(96f, 96f);
    private RectangleShape2D? _shape;

    [Export]
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            if (_shape is not null)
            {
                _shape.Size = value;
            }

            QueueRedraw();
            UpdateConfigurationWarnings();
        }
    }

    protected Rect2 LocalRect => new(-_size / 2f, _size);

    public override void _Ready()
    {
        _shape = new RectangleShape2D { Size = _size };
        AddChild(new CollisionShape2D { Shape = _shape });
    }
}
