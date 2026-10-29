using System.Collections.Generic;
using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Level;

namespace WarehouseRunner.Menu;

public partial class MainMenu : Node2D
{
    private static readonly Color ButtonIdle = new(0.12f, 0.13f, 0.16f, 0.85f);
    private static readonly Color ButtonFocus = new(0.95f, 0.55f, 0.12f, 0.95f);
    private static readonly Color Accent = new(1f, 0.82f, 0.1f);
    private static readonly Color Muted = new(0.75f, 0.78f, 0.82f);

    private const float PackageTileSize = 380f;

    private static int _lastSelectedIndex;
    private static int _lastPackageIndex;
    private static bool _packageOpen;

    private Label _subtitle = null!;
    private VBoxContainer _content = null!;
    private Label _skills = null!;
    private Label _footer = null!;

    public override void _Ready()
    {
        MusicPlayer.Play(MusicPlayer.MenuTheme);
        var layer = new CanvasLayer();
        AddChild(layer);

        var root = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        layer.AddChild(root);

        root.AddChild(CreateLabel("Warehouse Runner 2D", 64, Accent));
        _subtitle = CreateLabel("Level Select", 26, Colors.White);
        root.AddChild(_subtitle);
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 10f) });

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 8);
        root.AddChild(_content);

        _skills = CreateLabel(string.Empty, 22, new Color(0.85f, 0.88f, 0.92f));
        root.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f) });
        root.AddChild(_skills);
        _footer = CreateLabel(string.Empty, 18, new Color(0.7f, 0.72f, 0.76f));
        root.AddChild(_footer);
        layer.AddChild(CreateMuteButton(MusicPlayer.MusicBus, "Sound", rightOffset: -16f));
        layer.AddChild(CreateMuteButton(MusicPlayer.EffectsBus, "Effects", rightOffset: -182f));

        if (_packageOpen)
        {
            ShowLevels(_lastPackageIndex);
        }
        else
        {
            ShowPackages();
        }
    }

    public static void SelectLevel(string levelId)
    {
        var index = 0;
        for (var p = 0; p < LevelCatalog.Packages.Count; p++)
        {
            foreach (var level in LevelCatalog.Packages[p].Levels)
            {
                if (level.Id == levelId)
                {
                    _packageOpen = true;
                    _lastPackageIndex = p;
                    _lastSelectedIndex = index;
                    return;
                }

                index++;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_packageOpen && (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed(InputSetup.BackToMenu)))
        {
            GetViewport().SetInputAsHandled();
            ShowPackages();
        }
    }

    private void ShowPackages()
    {
        _packageOpen = false;
        ClearContent();
        _subtitle.Text = "Level Select";
        _skills.Text = string.Empty;
        _footer.Text = "Arrow keys: select     Enter/Space: open package";

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 40);
        _content.AddChild(row);
        var tiles = new List<Button>();
        for (var p = 0; p < LevelCatalog.Packages.Count; p++)
        {
            var tile = CreatePackageTile(p, LevelCatalog.Packages[p]);
            tiles.Add(tile);
            row.AddChild(tile);
        }

        tiles[Mathf.Clamp(_lastPackageIndex, 0, tiles.Count - 1)].GrabFocus();
    }

    private void ShowLevels(int packageIndex)
    {
        _packageOpen = true;
        _lastPackageIndex = packageIndex;
        ClearContent();
        var package = LevelCatalog.Packages[packageIndex];
        _subtitle.Text = $"Package {packageIndex + 1} · {package.Title}";
        _footer.Text = "Arrow keys: select     Enter/Space: start     Esc: back to packages";

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 8);
        list.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _content.AddChild(list);
        list.AddChild(CreateLabel(package.Description, 17, Muted));

        var firstIndex = 0;
        for (var p = 0; p < packageIndex; p++)
        {
            firstIndex += LevelCatalog.Packages[p].Levels.Count;
        }

        var buttons = new List<Button>();
        for (var i = 0; i < package.Levels.Count; i++)
        {
            var button = CreateLevelButton(firstIndex + i, $"{packageIndex + 1}-{i + 1}", package.Levels[i]);
            buttons.Add(button);
            list.AddChild(button);
        }

        list.AddChild(CreateBackButton());

        var selected = _lastSelectedIndex - firstIndex;
        buttons[selected >= 0 && selected < buttons.Count ? selected : 0].GrabFocus();
    }

    private void ClearContent()
    {
        foreach (var child in _content.GetChildren())
        {
            _content.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Button CreatePackageTile(int index, LevelPackage package)
    {
        var tile = new Button { CustomMinimumSize = new Vector2(PackageTileSize, PackageTileSize) };
        ApplyButtonStyle(tile);

        var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        text.AddThemeConstantOverride("separation", 14);
        tile.AddChild(text);
        text.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        text.AddChild(CreateLabel($"PACKAGE {index + 1}", 28, Accent));
        text.AddChild(CreateLabel(package.Title, 42, Colors.White));
        var description = CreateLabel(package.Description, 20, Muted);
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.CustomMinimumSize = new Vector2(PackageTileSize - 60f, 0f);
        text.AddChild(description);
        text.AddChild(CreateLabel($"{package.Levels.Count} Levels", 22, Colors.White));
        foreach (var child in text.GetChildren())
        {
            ((Control)child).MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        tile.FocusEntered += () => _lastPackageIndex = index;
        tile.MouseEntered += tile.GrabFocus;
        tile.Pressed += () => ShowLevels(index);
        return tile;
    }

    private static Button CreateMuteButton(string bus, string name, float rightOffset)
    {
        var button = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(150f, 44f) };
        ApplyButtonStyle(button);
        button.AddThemeFontSizeOverride("font_size", 20);
        void Refresh() => button.Text = MusicPlayer.IsMuted(bus) ? $"{name}: off" : $"{name}: on";
        Refresh();
        button.Pressed += () =>
        {
            MusicPlayer.SetMuted(bus, !MusicPlayer.IsMuted(bus));
            Refresh();
        };
        button.AnchorLeft = 1f;
        button.AnchorRight = 1f;
        button.OffsetLeft = rightOffset - 150f;
        button.OffsetRight = rightOffset;
        button.OffsetTop = 16f;
        button.OffsetBottom = 60f;
        return button;
    }

    private Button CreateBackButton()
    {
        var button = new Button
        {
            Text = "← Back to packages",
            CustomMinimumSize = new Vector2(600f, 40f),
            Alignment = HorizontalAlignment.Left,
        };
        ApplyButtonStyle(button);
        button.AddThemeFontSizeOverride("font_size", 19);
        button.FocusEntered += () => _skills.Text = string.Empty;
        button.MouseEntered += button.GrabFocus;
        button.Pressed += ShowPackages;
        return button;
    }

    private Button CreateLevelButton(int index, string number, LevelInfo level)
    {
        var best = RecordStore.LoadBestTime(level.Id);
        var bestText = best is { } seconds ? Hud.FormatTime(seconds) : "--:--.--";
        var medal = best is { } time ? level.Medals.For(time) : Medal.None;
        var button = new Button
        {
            Text = $"{number}  {level.Title}     {bestText}",
            CustomMinimumSize = new Vector2(600f, 48f),
            Alignment = HorizontalAlignment.Left,
        };
        button.AddChild(CreateMedalRow(medal));
        ApplyButtonStyle(button);
        button.AddThemeFontSizeOverride("font_size", 23);
        button.FocusEntered += () =>
        {
            RememberSelection(index);
            _skills.Text = $"{level.Skills}\n{MedalTimesText(level.Medals)}";
        };
        button.MouseEntered += button.GrabFocus;
        button.Pressed += () => GetTree().ChangeSceneToFile(level.ScenePath);
        return button;
    }

    private static void ApplyButtonStyle(Button button)
    {
        button.AddThemeStyleboxOverride("normal", CreateStyle(ButtonIdle));
        button.AddThemeStyleboxOverride("hover", CreateStyle(ButtonIdle.Lightened(0.15f)));
        button.AddThemeStyleboxOverride("pressed", CreateStyle(ButtonFocus));
        button.AddThemeStyleboxOverride("focus", CreateStyle(ButtonFocus));
        button.AddThemeColorOverride("font_focus_color", Colors.Black);
    }

    private static void RememberSelection(int index) => _lastSelectedIndex = index;

    private static string MedalTimesText(MedalTimes medals) =>
        $"Gold {Hud.FormatTime(medals.Gold)}   ·   Silver {Hud.FormatTime(medals.Silver)}   ·   Bronze {Hud.FormatTime(medals.Bronze)}";

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
