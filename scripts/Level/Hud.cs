using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Player;

namespace WarehouseRunner.Level;

public partial class Hud : CanvasLayer
{
    private const string ControlsText =
        "A/D oder Pfeile: Laufen\n" +
        "Leertaste/W: Springen (in der Luft: Doppelsprung, an Wand: Wall Kick)\n" +
        "Shift/J/S/Ctrl/K: Dash in der Luft, Rollout bei der Landung, Rolle im Laufen\n" +
        "Rennen, Richtung wechseln + Springen: Slide Jump\n" +
        "R: Neustart   Esc: Levelauswahl   F1: Hilfe ein/aus";

    private Label _timer = null!;
    private Label _objective = null!;
    private Label _best = null!;
    private Label _trick = null!;
    private Label _hint = null!;
    private Label _help = null!;
    private Label _result = null!;
    private ColorRect _resultDim = null!;
    private SpeedMeter _speedMeter = null!;
    private Tween? _trickTween;
    private Tween? _hintTween;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        _timer = AddLabel(root, Control.LayoutPreset.TopLeft, 44, new Vector2(24f, 16f));
        _best = AddLabel(root, Control.LayoutPreset.TopRight, 22, new Vector2(-24f, 20f), HorizontalAlignment.Right);
        _objective = AddLabel(root, Control.LayoutPreset.CenterTop, 26, new Vector2(0f, 24f), HorizontalAlignment.Center);
        _trick = AddLabel(root, Control.LayoutPreset.CenterTop, 34, new Vector2(0f, 130f), HorizontalAlignment.Center);
        _hint = AddLabel(root, Control.LayoutPreset.CenterTop, 24, new Vector2(0f, 70f), HorizontalAlignment.Center);
        _help = AddLabel(root, Control.LayoutPreset.BottomRight, 16, new Vector2(-24f, -20f), HorizontalAlignment.Right);

        _resultDim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.55f), Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_resultDim);
        _resultDim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _result = AddLabel(root, Control.LayoutPreset.Center, 40, Vector2.Zero, HorizontalAlignment.Center);

        _help.Text = ControlsText;
        _trick.Modulate = Colors.Transparent;
        _hint.Modulate = Colors.Transparent;
        _result.Visible = false;

        _speedMeter = new SpeedMeter();
        _speedMeter.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        _speedMeter.OffsetLeft = 24f;
        _speedMeter.OffsetRight = 284f;
        _speedMeter.OffsetTop = -56f;
        _speedMeter.OffsetBottom = -26f;
        root.AddChild(_speedMeter);
    }

    public void ToggleHelp() => _help.Visible = !_help.Visible;

    public void SetTime(double seconds) => _timer.Text = FormatTime(seconds);

    public void SetObjective(string text) => _objective.Text = text;

    public void SetBest(double? seconds, MedalTimes? medals)
    {
        var bestLine = seconds is { } best ? $"Bestzeit {FormatTime(best)}" : "Bestzeit --:--.--";
        if (medals is null)
        {
            _best.Text = bestLine;
            return;
        }

        var earned = seconds is { } time ? medals.For(time) : Medal.None;
        var next = MedalTimes.NextAfter(earned);
        var goalLine = next is { } goal
            ? $"Ziel {MedalTimes.Name(goal)} {FormatTime(medals.TimeFor(goal))}"
            : "Alle Medaillen geholt!";
        _best.Text = $"{bestLine}\n{goalLine}";
    }

    public void SetSpeed(float speed, float runSpeed, bool boosted) => _speedMeter.Update(speed, runSpeed, boosted);

    public void ShowTrick(Trick trick, bool perfect)
    {
        var name = TrickName(trick);
        if (name is null)
        {
            return;
        }

        _trick.Text = perfect ? $"{name}  PERFEKT!" : name;
        _trick.AddThemeColorOverride("font_color", perfect ? new Color(1f, 0.85f, 0.2f) : Colors.White);
        _trickTween?.Kill();
        _trick.Modulate = Colors.White;
        _trickTween = CreateTween();
        _trickTween.TweenInterval(0.5f);
        _trickTween.TweenProperty(_trick, "modulate", Colors.Transparent, 0.4f);
    }

    public void ShowHint(string text)
    {
        _hint.Text = text;
        _hintTween?.Kill();
        _hint.Modulate = Colors.White;
        _hintTween = CreateTween();
        _hintTween.TweenInterval(2f);
        _hintTween.TweenProperty(_hint, "modulate", Colors.Transparent, 0.5f);
    }

    public void ShowResult(double time, double? previousBest, bool hasNextLevel, MedalTimes? medals)
    {
        var isRecord = previousBest is null || time < previousBest;
        var recordLine = isRecord ? "NEUER REKORD!" : $"Bestzeit: {FormatTime(previousBest!.Value)}";
        var medalLines = medals is null ? string.Empty : MedalResultLines(time, medals);
        _result.Text = $"Feierabend!\n{FormatTime(time)}\n{recordLine}{medalLines}\n\n{(hasNextLevel ? "Enter / A: nächstes Level" : "Enter / A: zum Startbildschirm")}\nR: neuer Lauf   Esc: Levelauswahl";
        _result.Visible = true;
        _resultDim.Visible = true;
    }

    private static string MedalResultLines(double time, MedalTimes medals)
    {
        var medal = medals.For(time);
        var earnedLine = medal == Medal.None ? "Keine Medaille" : $"{MedalTimes.Name(medal).ToUpperInvariant()}-MEDAILLE!";
        var next = MedalTimes.NextAfter(medal);
        var nextLine = next is { } goal ? $"\n{MedalTimes.Name(goal)}: {FormatTime(medals.TimeFor(goal))}" : string.Empty;
        return $"\n\n{earnedLine}{nextLine}";
    }

    public static string FormatTime(double seconds)
    {
        var minutes = (int)(seconds / 60);
        return $"{minutes:00}:{seconds - (minutes * 60):00.00}";
    }

    private static string? TrickName(Trick trick) => trick switch
    {
        Trick.DoubleJump => "Doppelsprung",
        Trick.SlideJump => "Slide Jump",
        Trick.Dash => "Dash",
        Trick.DashJump => "Dash Jump",
        Trick.WallKick => "Wall Kick",
        Trick.JumpRollout => "Rollout!",
        Trick.DashRollout => "Dash-Rollout!",
        Trick.GroundRoll => "Rolle",
        Trick.RollJump => "Roll Jump",
        Trick.PowerDash => "Power-Dash!",
        Trick.SlopeSlide => "Rutsch-Boost",
        _ => null,
    };

    private static Label AddLabel(Control parent, Control.LayoutPreset preset, int fontSize, Vector2 offset, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            HorizontalAlignment = alignment,
            LabelSettings = new LabelSettings { FontSize = fontSize, OutlineSize = 6, OutlineColor = Colors.Black },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(label);
        label.SetAnchorsAndOffsetsPreset(preset, Control.LayoutPresetMode.Minsize);
        label.GrowHorizontal = alignment switch
        {
            HorizontalAlignment.Right => Control.GrowDirection.Begin,
            HorizontalAlignment.Center => Control.GrowDirection.Both,
            _ => Control.GrowDirection.End,
        };
        label.GrowVertical = preset is Control.LayoutPreset.BottomRight
            ? Control.GrowDirection.Begin
            : Control.GrowDirection.Both;
        label.Position += offset;
        return label;
    }
}
