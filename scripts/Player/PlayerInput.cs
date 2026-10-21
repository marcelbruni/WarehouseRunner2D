using Godot;
using WarehouseRunner.Core;

namespace WarehouseRunner.Player;

public readonly record struct PlayerInput(
    float MoveX,
    bool JumpPressed,
    bool JumpHeld,
    bool ActionPressed);

public interface IPlayerInputSource
{
    PlayerInput Sample();
}

public sealed class LocalInputSource : IPlayerInputSource
{
    public PlayerInput Sample() => new(
        Input.GetAxis(InputSetup.MoveLeft, InputSetup.MoveRight),
        Input.IsActionJustPressed(InputSetup.Jump),
        Input.IsActionPressed(InputSetup.Jump),
        Input.IsActionJustPressed(InputSetup.DashOrRoll));
}
