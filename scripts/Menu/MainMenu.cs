using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Level;

namespace WarehouseRunner.Menu;

public partial class MainMenu : Node2D
{
    private static readonly Color ButtonIdle = new(0.12f, 0.13f, 0.16f, 0.85f);
    private static readonly Color ButtonFocus = new(0.95f, 0.55f, 0.12f, 0.95f);
    private static readonly Color Accent = new(1f, 0.82f, 0.1f);

    private static int _lastSelectedIndex;

    private Label _skills = null!;

    public override void _Ready()
    {
        var layer = new CanvasLayer();
        AddChild(layer);

        var root = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 14);
        layer.AddChild(root);

        root.AddChild(CreateLabel("WAREHOUSE RUNNER", 64, Accent));
        root.AddChild(CreateLabel("Levelauswahl", 26, Colors.White));
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 10f) });

        var buttons = new Button[LevelCatalog.Levels.Count];
        for (var i = 0; i < LevelCatalog.Levels.Count; i++)
        {
            buttons[i] = CreateLevelButton(i, LevelCatalog.Levels[i]);
            root.AddChild(CenterInRow(buttons[i]));
        }

        _skills = CreateLabel(string.Empty, 22, new Color(0.85f, 0.88f, 0.92f));
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f) });
        root.AddChild(_skills);
        root.AddChild(CreateLabel("Pfeiltasten: auswählen     Enter/Leertaste: starten", 18, new Color(0.7f, 0.72f, 0.76f)));

        buttons[Mathf.Clamp(_lastSelectedIndex, 0, buttons.Length - 1)].GrabFocus();
    }

    private Button CreateLevelButton(int index, LevelInfo level)
    {
        var best = RecordStore.LoadBestTime(level.Id);
        var bestText = best is { } seconds ? Hud.FormatTime(seconds) : "--:--.--";
        var button = new Button
        {
            Text = $"{index + 1}.  {level.Title}        Bestzeit {bestText}",
            CustomMinimumSize = new Vector2(760f, 56f),
            Alignment = HorizontalAlignment.Left,
        };
        button.AddThemeFontSizeOverride("font_size", 26);
        button.AddThemeStyleboxOverride("normal", CreateStyle(ButtonIdle));
        button.AddThemeStyleboxOverride("hover", CreateStyle(ButtonIdle.Lightened(0.15f)));
        button.AddThemeStyleboxOverride("pressed", CreateStyle(ButtonFocus));
        button.AddThemeStyleboxOverride("focus", CreateStyle(ButtonFocus));
        button.AddThemeColorOverride("font_focus_color", Colors.Black);
        button.FocusEntered += () =>
        {
            RememberSelection(index);
            _skills.Text = level.Skills;
        };
        button.MouseEntered += button.GrabFocus;
        button.Pressed += () => GetTree().ChangeSceneToFile(level.ScenePath);
        return button;
    }

    private static void RememberSelection(int index) => _lastSelectedIndex = index;

    private static CenterContainer CenterInRow(Control control)
    {
        var row = new CenterContainer();
        row.AddChild(control);
        return row;
    }

    private static Label CreateLabel(string text, int fontSize, Color color) => new()
    {
        Text = text,
        HorizontalAlignment = HorizontalAlignment.Center,
        LabelSettings = new LabelSettings { FontSize = fontSize, FontColor = color, OutlineSize = 6, OutlineColor = Colors.Black },
    };

    private static StyleBoxFlat CreateStyle(Color color) => new()
    {
        BgColor = color,
        CornerRadiusTopLeft = 6,
        CornerRadiusTopRight = 6,
        CornerRadiusBottomLeft = 6,
        CornerRadiusBottomRight = 6,
        ContentMarginLeft = 20f,
        ContentMarginRight = 20f,
    };
}
