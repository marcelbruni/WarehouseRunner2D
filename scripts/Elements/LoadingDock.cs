using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class LoadingDock : PlayerTrigger
{
    private const float DoorWidth = 230f;
    private const float DoorHeight = 215f;
    private static readonly Vector2 CargoSpot = new(0f, -70f);

    private static readonly Color Frame = new(0.22f, 0.23f, 0.25f);
    private static readonly Color Yard = new(0.55f, 0.66f, 0.78f);
    private static readonly Color Trailer = new(0.9f, 0.9f, 0.88f);
    private static readonly Color TrailerDark = new(0.5f, 0.5f, 0.5f);
    private static readonly Color CargoSpace = new(0.15f, 0.15f, 0.17f);
    private static readonly Color RollDoor = new(0.62f, 0.64f, 0.67f);
    private static readonly Color Hazard = new(0.95f, 0.78f, 0.12f);

    private Vector2 _packageFlightStart;
    private float _packageFlight = -1f;
    private float _trailerDoorsClosed;
    private float _truckDistance;
    private float _rollDoorClosed;
    private bool _packageInside;

    public LoadingDock()
    {
        TriggerSize = new Vector2(180f, 600f);
        ZIndex = -1;
    }

    protected override Vector2 TriggerCenter => new(0f, -TriggerSize.Y / 2f);

    public void DispatchTruck(Vector2 packageGlobalPosition)
    {
        _packageFlightStart = ToLocal(packageGlobalPosition);
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(SetPackageFlight), 0f, 1f, 0.4f);
        tween.TweenCallback(Callable.From(() =>
        {
            _packageFlight = -1f;
            _packageInside = true;
        }));
        tween.TweenMethod(Callable.From<float>(v => _trailerDoorsClosed = v), 0f, 1f, 0.3f);
        tween.TweenMethod(Callable.From<float>(v => _truckDistance = v), 0f, 1f, 1.4f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenMethod(Callable.From<float>(v => _rollDoorClosed = v), 0f, 1f, 0.6f);
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var opening = new Rect2(-DoorWidth / 2f, -DoorHeight, DoorWidth, DoorHeight);
        DrawRect(opening.Grow(14f), Frame);
        DrawRect(opening, Yard);
        DrawString(ThemeDB.FallbackFont, new Vector2(-48f, -DoorHeight - 22f), "DOCK 3", HorizontalAlignment.Left, -1, 20, Hazard);

        DrawTruck();
        DrawFlyingPackage();
        DrawRollDoor(opening);
        DrawBumpers(opening);
    }

    private void SetPackageFlight(float progress) => _packageFlight = progress;

    private void DrawTruck()
    {
        if (_truckDistance >= 1f)
        {
            return;
        }

        var scale = 1f - (_truckDistance * 0.75f);
        var alpha = 1f - _truckDistance;
        DrawSetTransform(new Vector2(0f, -10f - (_truckDistance * 60f)), 0f, Vector2.One * scale);

        var body = new Rect2(-100f, -190f, 200f, 180f);
        DrawRect(body, Trailer with { A = alpha });
        var cargo = body.Grow(-12f);
        DrawRect(cargo, CargoSpace with { A = alpha });
        if (_packageInside)
        {
            PackageArt.Draw(this, CargoSpot + new Vector2(0f, -30f), 34f);
        }

        var doorWidth = (cargo.Size.X / 2f) * _trailerDoorsClosed;
        DrawRect(new Rect2(cargo.Position, new Vector2(doorWidth, cargo.Size.Y)), Trailer with { A = alpha });
        DrawRect(new Rect2(cargo.End.X - doorWidth, cargo.Position.Y, doorWidth, cargo.Size.Y), Trailer with { A = alpha });
        DrawRect(body, TrailerDark with { A = alpha }, false, 4f);
        DrawRect(new Rect2(-96f, -24f, 14f, 10f), new Color(0.9f, 0.1f, 0.1f, alpha));
        DrawRect(new Rect2(82f, -24f, 14f, 10f), new Color(0.9f, 0.1f, 0.1f, alpha));

        DrawSetTransform(Vector2.Zero);
    }

    private void DrawFlyingPackage()
    {
        if (_packageFlight < 0f)
        {
            return;
        }

        var target = CargoSpot + new Vector2(0f, -40f);
        var position = _packageFlightStart.Lerp(target, _packageFlight);
        position.Y -= Mathf.Sin(_packageFlight * Mathf.Pi) * 80f;
        PackageArt.Draw(this, position, 34f);
    }

    private void DrawRollDoor(Rect2 opening)
    {
        if (_rollDoorClosed <= 0f)
        {
            return;
        }

        var door = new Rect2(opening.Position, new Vector2(opening.Size.X, opening.Size.Y * _rollDoorClosed));
        DrawRect(door, RollDoor);
        for (var y = door.Position.Y + 12f; y < door.End.Y; y += 12f)
        {
            DrawLine(new Vector2(door.Position.X, y), new Vector2(door.End.X, y), TrailerDark, 1.5f);
        }
    }

    private void DrawBumpers(Rect2 opening)
    {
        var stripe = 14f;
        for (var y = -DoorHeight; y < 0f; y += stripe * 2f)
        {
            DrawRect(new Rect2(opening.Position.X - 14f, y, 14f, stripe), Hazard);
            DrawRect(new Rect2(opening.End.X, y, 14f, stripe), Hazard);
        }
    }
}
