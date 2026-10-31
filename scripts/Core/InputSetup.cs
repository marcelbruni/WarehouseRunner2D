using Godot;

namespace WarehouseRunner.Core;

public partial class InputSetup : Node
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Jump = "jump";
    public const string DashOrRoll = "dash_or_roll";
    public const string Crouch = "crouch";
    public const string Dash = "dash";
    public const string AimUp = "aim_up";
    public const string AimDown = "aim_down";
    public const string Restart = "restart";
    public const string ToggleHelp = "toggle_help";
    public const string BackToMenu = "back_to_menu";
    public const string Continue = "continue";

    public override void _EnterTree()
    {
        Register(MoveLeft, Keys(Key.A, Key.Left), Buttons(JoyButton.DpadLeft), new Axis(JoyAxis.LeftX, -1f));
        Register(MoveRight, Keys(Key.D, Key.Right), Buttons(JoyButton.DpadRight), new Axis(JoyAxis.LeftX, 1f));
        Register(Jump, Keys(Key.Space), Buttons(JoyButton.A));
        Register(DashOrRoll, Keys(Key.Shift, Key.J, Key.Ctrl, Key.K), Buttons(JoyButton.X, JoyButton.B));
        Register(Crouch, Keys(Key.Down, Key.S), Buttons(), new Axis(JoyAxis.TriggerLeft, 1f));
        Register(Dash, Keys(), Buttons(), new Axis(JoyAxis.TriggerLeft, 1f));
        Register(AimUp, Keys(Key.Up, Key.W), Buttons(), new Axis(JoyAxis.LeftY, -1f));
        Register(AimDown, Keys(Key.Down, Key.S), Buttons(), new Axis(JoyAxis.LeftY, 1f));
        Register(Restart, Keys(Key.R), Buttons(JoyButton.Start));
        Register(ToggleHelp, Keys(Key.F1), Buttons());
        Register(BackToMenu, Keys(Key.Escape), Buttons(JoyButton.Back));
        Register(Continue, Keys(Key.Enter, Key.KpEnter), Buttons(JoyButton.A, JoyButton.Y));

        EnsureJoypadButton("ui_accept", JoyButton.A);
        EnsureJoypadButton("ui_up", JoyButton.DpadUp);
        EnsureJoypadButton("ui_down", JoyButton.DpadDown);
        EnsureJoypadButton("ui_left", JoyButton.DpadLeft);
        EnsureJoypadButton("ui_right", JoyButton.DpadRight);
        EnsureKey("ui_up", Key.W);
        EnsureKey("ui_down", Key.S);
    }

    private static void EnsureKey(string action, Key key)
    {
        var inputEvent = new InputEventKey { PhysicalKeycode = key };
        if (!InputMap.ActionHasEvent(action, inputEvent))
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }
    }

    private static void EnsureJoypadButton(string action, JoyButton button)
    {
        var inputEvent = new InputEventJoypadButton { ButtonIndex = button };
        if (!InputMap.ActionHasEvent(action, inputEvent))
        {
            InputMap.ActionAddEvent(action, inputEvent);
        }
    }

    private readonly record struct Axis(JoyAxis Id, float Direction);

    private static Key[] Keys(params Key[] keys) => keys;

    private static JoyButton[] Buttons(params JoyButton[] buttons) => buttons;

    private static void Register(string action, Key[] keys, JoyButton[] buttons, Axis? axis = null)
    {
        if (InputMap.HasAction(action))
        {
            return;
        }

        InputMap.AddAction(action, 0.4f);
        foreach (var key in keys)
        {
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }

        foreach (var button in buttons)
        {
            InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = button });
        }

        if (axis is { } a)
        {
            InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = a.Id, AxisValue = a.Direction });
        }
    }
}
