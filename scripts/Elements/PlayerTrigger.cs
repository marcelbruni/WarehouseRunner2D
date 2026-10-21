using System;
using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
public partial class PlayerTrigger : Area2D
{
    private Vector2 _triggerSize = new(48f, 120f);
    private RectangleShape2D? _shape;
    private CollisionShape2D? _collision;

    public event Action<PlayerController>? PlayerEntered;

    [Export]
    public Vector2 TriggerSize
    {
        get => _triggerSize;
        set
        {
            _triggerSize = value;
            UpdateShape();
            QueueRedraw();
        }
    }

    protected virtual Vector2 TriggerCenter => Vector2.Zero;

    public override void _Ready()
    {
        _shape = new RectangleShape2D();
        _collision = new CollisionShape2D { Shape = _shape };
        AddChild(_collision);
        UpdateShape();

        if (!Engine.IsEditorHint())
        {
            BodyEntered += OnBodyEntered;
        }
    }

    protected void UpdateShape()
    {
        if (_shape is null || _collision is null)
        {
            return;
        }

        _shape.Size = _triggerSize;
        _collision.Position = TriggerCenter;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerController player)
        {
            PlayerEntered?.Invoke(player);
        }
    }
}
