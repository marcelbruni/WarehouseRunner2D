using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class ExpressPackage : PlayerTrigger
{
    private const float BoxSize = 44f;

    private float _time;

    public ExpressPackage()
    {
        TriggerSize = new Vector2(56f, 56f);
    }

    public void Collect()
    {
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
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
    }
}
