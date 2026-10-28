using Godot;
using WarehouseRunner.Player;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class ExpressPackage : PlayerTrigger
{
    private const float BoxSize = 44f;
    private const float FleeDuration = 0.45f;
    private const float RemarkDuration = 1.2f;

    private static readonly string[] Remarks = ["Nope!", "Catch me if you can!", "I'm a free package!", "Fine..."];

    private float _time;
    private int _nextFleeIndex;
    private bool _isFleeing;
    private float _remarkTimer;
    private string _remark = string.Empty;

    public ExpressPackage()
    {
        TriggerSize = new Vector2(56f, 56f);
    }

    [Export]
    public Vector2[] FleePath { get; set; } = [];

    [Export]
    public float FleeRadius { get; set; } = 150f;

    public void Collect()
    {
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || _isFleeing || _nextFleeIndex >= FleePath.Length || !Visible)
        {
            return;
        }

        foreach (var node in GetTree().GetNodesInGroup(PlayerController.PlayerGroup))
        {
            if (node is PlayerController player && player.GlobalPosition.DistanceTo(GlobalPosition) < FleeRadius)
            {
                FleeTo(FleePath[_nextFleeIndex]);
                return;
            }
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _remarkTimer -= (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var bob = Mathf.Sin(_time * 3f) * 5f;
        var glow = (Mathf.Sin(_time * 5f) + 1f) / 2f;
        PackageArt.Draw(this, new Vector2(0f, bob), BoxSize, glow);

        for (var i = 0; i < 4; i++)
        {
            var angle = (_time * 1.6f) + (i * Mathf.Tau / 4f);
            var sparkle = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 40f;
            DrawCircle(sparkle + new Vector2(0f, bob), 2.5f, new Color(1f, 1f, 0.7f, 0.8f));
        }

        if (FleePath.Length > 0)
        {
            DrawEyes(bob);
        }

        if (_remarkTimer > 0f)
        {
            DrawString(ThemeDB.FallbackFont, new Vector2(-60f, -50f + bob), _remark, HorizontalAlignment.Center, 120f, 16, Colors.White);
        }
    }

    private void FleeTo(Vector2 target)
    {
        _isFleeing = true;
        _remark = Remarks[Mathf.Min(_nextFleeIndex, Remarks.Length - 1)];
        _remarkTimer = RemarkDuration;
        _nextFleeIndex++;
        if (_nextFleeIndex >= FleePath.Length)
        {
            _remark = Remarks[^1];
        }

        var tween = CreateTween();
        tween.TweenProperty(this, Node2D.PropertyName.GlobalPosition.ToString(), target, FleeDuration)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => _isFleeing = false));
    }

    private void DrawEyes(float bob)
    {
        foreach (var x in new[] { -9f, 9f })
        {
            var eye = new Vector2(x, -6f + bob);
            DrawCircle(eye, 5f, Colors.White);
            DrawCircle(eye + new Vector2(Mathf.Sin(_time * 2f) * 2f, 1f), 2.2f, Colors.Black);
        }
    }
}
