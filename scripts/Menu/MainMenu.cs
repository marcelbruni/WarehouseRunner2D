using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Level;

namespace WarehouseRunner.Menu;

public partial class MainMenu : Node2D
{
    private static readonly Color ButtonIdle = new(0.12f, 0.13f, 0.16f, 0.85f);
    private static readonly Color ButtonFocus = new(0.95f, 0.55f, 0.12f, 0.95f);
    private static readonly Color Accent = new(1f, 0.82f, 0.1f);

    private const int ColumnCount = 2;

    private static int _lastSelectedIndex;

    private Label _skills = null!;

    public override void _Ready()
    {
        MusicPlayer.Play(MusicPlayer.MenuTheme);
        var layer = new CanvasLayer();
        AddChild(layer);

        var root = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 14);
        layer.AddChild(root);

        root.AddChild(CreateLabel("Warehouse Runner 2D", 64, Accent));
        root.AddChild(CreateLabel("Levelauswahl", 26, Colors.White));
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 10f) });

        var columns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        columns.AddThemeConstantOverride("separation", 20);
        root.AddChild(columns);
        var rowsPerColumn = Mathf.CeilToInt(LevelCatalog.Levels.Count / (float)ColumnCount);
        VBoxContainer? column = null;
        var buttons = new Button[LevelCatalog.Levels.Count];
        for (var i = 0; i < LevelCatalog.Levels.Count; i++)
        {
            if (i % rowsPerColumn == 0)
            {
                column = new VBoxContainer();
                column.AddThemeConstantOverride("separation", 10);
                columns.AddChild(column);
            }

            buttons[i] = CreateLevelButton(i, LevelCatalog.Levels[i]);
            column!.AddChild(buttons[i]);
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
        var medal = best is { } time ? level.Medals.For(time) : Medal.None;
        var button = new Button
        {
            Text = $"{index + 1}.  {level.Title}     {bestText}",
            CustomMinimumSize = new Vector2(600f, 52f),
            Alignment = HorizontalAlignment.Left,
        };
        button.AddChild(CreateMedalRow(medal));
        button.AddThemeFontSizeOverride("font_size", 23);
        button.AddThemeStyleboxOverride("normal", CreateStyle(ButtonIdle));
        button.AddThemeStyleboxOverride("hover", CreateStyle(ButtonIdle.Lightened(0.15f)));
        button.AddThemeStyleboxOverride("pressed", CreateStyle(ButtonFocus));
        button.AddThemeStyleboxOverride("focus", CreateStyle(ButtonFocus));
        button.AddThemeColorOverride("font_focus_color", Colors.Black);
        button.FocusEntered += () =>
        {
            RememberSelection(index);
            _skills.Text = $"{level.Skills}\n{MedalTimesText(level.Medals)}";
        };
        button.MouseEntered += button.GrabFocus;
        button.Pressed += () => GetTree().ChangeSceneToFile(level.ScenePath);
        return button;
    }

    private static void RememberSelection(int index) => _lastSelectedIndex = index;

    private static string MedalTimesText(MedalTimes medals) =>
        $"Gold {Hud.FormatTime(medals.Gold)}   ·   Silber {Hud.FormatTime(medals.Silver)}   ·   Bronze {Hud.FormatTime(medals.Bronze)}";


    private static HBoxContainer CreateMedalRow(Medal earned)
    {
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
        row.AddThemeConstantOverride("separation", 4);
        foreach (var medal in new[] { Medal.Bronze, Medal.Silver, Medal.Gold })
        {
            row.AddChild(MedalBadge.Create(medal, earned >= medal, 30f));
        }

        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
        row.GrowHorizontal = Control.GrowDirection.Begin;
        row.Position += new Vector2(-14f, 0f);
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
