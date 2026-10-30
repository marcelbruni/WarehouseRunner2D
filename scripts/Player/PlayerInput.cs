using Godot;
using WarehouseRunner.Core;

namespace WarehouseRunner.Player;

public readonly record struct PlayerInput(
    float MoveX,
    bool JumpPressed,
    bool JumpHeld,
    bool ActionPressed,
    bool CrouchHeld = false,
    float AimY = 0f,
    bool DashPressed = false);

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
        Input.IsActionJustPressed(InputSetup.DashOrRoll),
        Input.IsActionPressed(InputSetup.Crouch),
        Input.GetAxis(InputSetup.AimUp, InputSetup.AimDown),
        Input.IsActionJustPressed(InputSetup.Dash));
}
