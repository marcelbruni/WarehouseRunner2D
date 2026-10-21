using System.Collections.Generic;
using Godot;

namespace WarehouseRunner.Player;

public partial class AfterimageTrail : Node2D
{
    private const int MaxImages = 8;
    private const float RecordInterval = 0.03f;
    private const float ImageLifetime = 0.25f;

    private readonly LinkedList<(Vector2 Position, float Rotation, float Age)> _images = new();
    private PlayerController _player = null!;
    private float _recordTimer;

    public override void _Ready()
    {
        TopLevel = true;
        ZIndex = -1;
        _player = GetParent<PlayerController>();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var node = _images.First;
        while (node is not null)
        {
            var next = node.Next;
            node.ValueRef.Age += dt;
            if (node.Value.Age >= ImageLifetime)
            {
                _images.Remove(node);
            }

            node = next;
        }

        _recordTimer -= dt;
        var shouldRecord = _player.State is PlayerState.Dashing or PlayerState.RollingOut || _player.IsBoosted;
        if (shouldRecord && _recordTimer <= 0f)
        {
            _recordTimer = RecordInterval;
            var visual = _player.GetNode<Node2D>("Visual");
            _images.AddLast((_player.GlobalPosition, visual.Rotation, 0f));
            if (_images.Count > MaxImages)
            {
                _images.RemoveFirst();
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var image in _images)
        {
            var alpha = 0.35f * (1f - (image.Age / ImageLifetime));
            DrawSetTransform(image.Position, image.Rotation);
            DrawRect(new Rect2(-11f, -30f, 22f, 56f), new Color(1f, 0.55f, 0.15f, alpha));
        }

        DrawSetTransform(Vector2.Zero);
    }
}
