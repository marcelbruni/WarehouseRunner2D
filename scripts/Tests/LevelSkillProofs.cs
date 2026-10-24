using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Elements;
using WarehouseRunner.Player;

namespace WarehouseRunner.Tests;

public partial class LevelSkillProofs : Node
{
    private const float StandY = -26f;

    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");

    private const string OnlyArgument = "--only=";
    private const string TraceArgument = "--trace";

    private readonly List<string> _failures = new();
    private readonly HashSet<PlayerController> _scanned = new();
    private string? _only;
    private bool _trace;

    private enum Opener
    {
        WalkOff,
        RunningJump,
        SlideJump,
        RollJump,
        Roll,
        JumpThenRoll,
        Hopper,
    }

    private sealed record Plan(Opener Opener, float StartX, float StartY, float TriggerX, int RunBackTicks, int? DoubleJumpTick, int? DashTick, int DelayTicks = 0, float Dir = 1f)
    {
        public string Describe() => $"{Opener} start {StartX:0} trigger {TriggerX:0} back {RunBackTicks} dj {DoubleJumpTick?.ToString() ?? "-"} dash {DashTick?.ToString() ?? "-"} delay {DelayTicks}";
    }

    private sealed record Section(string Name, string ScenePath, Func<PlayerController, bool> Reached, int Ticks, float DiesBelowY = float.MaxValue, bool ScannersAlwaysOn = false);

    public override async void _Ready()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            _trace |= argument == TraceArgument;
            if (argument.StartsWith(OnlyArgument))
            {
                _only = argument[OnlyArgument.Length..];
            }
        }

        await ProveLevel01();
        await ProveRollLevel();
        await ProveDashLevel();
        await ProveLevel04();
        await ProveAiLevel();
        await ProveFriendsLevel();
        await ProveSlideLevel();
        await ProveNightShiftLevel();
        await ProveClimbingHallLevel();

        foreach (var failure in _failures)
        {
            GD.PrintErr(failure);
        }

        GD.Print(_failures.Count == 0 ? "PASS LevelSkillProofs" : "FAIL LevelSkillProofs");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private async Task ProveLevel01()
    {
        const string scene = "res://scenes/levels/Level01.tscn";

        var lowCrate = new Section("L1 Kiste 100 (Sprung)", scene, p => p.GlobalPosition.X > 300f && p.IsOnGround, 240);
        await Prove(lowCrate, required: Jumps(Opener.RunningJump, 0f, 60f, 130f, withDoubleJump: false, withDash: false),
            insufficient: [new Plan(Opener.WalkOff, 0f, StandY, 0f, 0, null, null)]);

        var highCrate = new Section("L1 Kiste 200 (Doppelsprung)", scene, p => p.GlobalPosition.X > 800f && p.IsOnGround, 300);
        await Prove(highCrate, required: Jumps(Opener.RunningJump, 450f, 520f, 620f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 450f, 500f, 640f, withDoubleJump: false, withDash: false));

        var ledge = new Section("L1 Kante 290 (Slide Jump)", scene, p => p.GlobalPosition.Y < -300f && p.IsOnGround, 360);
        await Prove(ledge, required: SlideJumps(1300f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 900f, 1100f, 1340f, withDoubleJump: true, withDash: false)
                .Concat(SlideJumps(1300f, withDoubleJump: false, withDash: false)));

        var chimney = new Section("L1 Kamin 560 (Wall Kick)", scene, p => p.GlobalPosition.X > 2460f, 900);
        await ProveWallClimb(chimney);
    }

    private async Task ProveDashLevel()
    {
        const string scene = "res://scenes/levels/Level03.tscn";

        var tunnelGap = new Section("L3 Lücke unter Decke (Sprung + Dash)", scene, p => p.GlobalPosition.X > 2670f && p.IsOnGround && p.GlobalPosition.Y < 0f, 300, DiesBelowY: 40f);
        await Prove(tunnelGap, required: Jumps(Opener.RunningJump, 2120f, 2320f, 2420f, withDoubleJump: false, withDash: true),
            insufficient: Jumps(Opener.RunningJump, 2120f, 2320f, 2420f, withDoubleJump: true, withDash: false)
                .Append(new Plan(Opener.WalkOff, 2120f, StandY, 0f, 0, null, null)));

        var wideGap = new Section("L3 Lücke 600 (Doppelsprung + Dash)", scene, p => p.GlobalPosition.X > 1020f && p.IsOnGround && p.GlobalPosition.Y < 0f, 360);
        await Prove(wideGap, required: Jumps(Opener.RunningJump, 50f, 300f, 400f, withDoubleJump: true, withDash: true),
            insufficient: Jumps(Opener.RunningJump, 50f, 300f, 400f, withDoubleJump: true, withDash: false)
                .Concat(Jumps(Opener.RunningJump, 50f, 300f, 400f, withDoubleJump: false, withDash: true))
                .Concat(SlideJumps(350f, withDoubleJump: true, withDash: false))
                .Concat(SlideJumps(350f, withDoubleJump: false, withDash: true)));

        var ceilingTop = new Section("L3 Decke nicht überspringbar", scene, p => p.IsOnGround && p.GlobalPosition.Y < -900f, 360);
        await ProveImpossible(ceilingTop,
            Jumps(Opener.RunningJump, 1550f, 1700f, 1890f, withDoubleJump: true, withDash: true)
                .Concat(Jumps(Opener.RollJump, 1550f, 1700f, 1890f, withDoubleJump: true, withDash: true))
                .Concat(SlideJumps(1850f, withDoubleJump: true, withDash: true))
                .Select(plan => plan with { StartY = -326f }));

        var ledge = new Section("L3 Kante 300 (Slide Jump + Doppelsprung)", scene, p => p.GlobalPosition.Y < -310f && p.IsOnGround, 360);
        await Prove(ledge, required: SlideJumps(1430f, withDoubleJump: true, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 1000f, 1250f, 1470f, withDoubleJump: true, withDash: false));
    }

    private async Task ProveRollLevel()
    {
        const string scene = "res://scenes/levels/Level02.tscn";
        foreach (var (name, startX, shelfStart, shelfEnd) in new[] { ("L2 Regal 1 (Rolle)", -100f, 200f, 500f), ("L2 Regal 3 (Rolle)", 1750f, 1950f, 2550f) })
        {
            var shelf = new Section(name, scene, p => p.GlobalPosition.X > shelfEnd + 20f, 400);
            await Prove(shelf, required: [new Plan(Opener.Roll, startX, StandY, shelfStart - 60f, 0, null, null)],
                insufficient: Jumps(Opener.RunningJump, startX, startX + 20f, shelfStart - 20f, withDoubleJump: true, withDash: false)
                    .Append(new Plan(Opener.WalkOff, startX, StandY, 0f, 0, null, null)));
        }

        var shelfAfterCrate = new Section("L2 Regal 2 nach Kiste (Rollout)", scene, p => p.GlobalPosition.X > 1670f, 400);
        await Prove(shelfAfterCrate, required: Jumps(Opener.JumpThenRoll, 700f, 800f, 880f, withDoubleJump: false, withDash: false),
            insufficient: Jumps(Opener.RunningJump, 700f, 800f, 880f, withDoubleJump: true, withDash: false));
    }

    private async Task ProveLevel04()
    {
        const string scene = "res://scenes/levels/Level04.tscn";
        foreach (var (name, startX, edgeX, landX) in new[] { ("L4 Lücke A 840 (Roll Jump + Power-Dash + Sprung)", -150f, 400f, 1250f), ("L4 Lücke B 840", 1260f, 1740f, 2590f) })
        {
            var gap = new Section(name, scene, p => p.GlobalPosition.X > landX && p.IsOnGround && p.GlobalPosition.Y < 0f, 420);
            await Prove(gap, required: Jumps(Opener.RollJump, startX, (edgeX - 120f), (edgeX - 10f), withDoubleJump: true, withDash: true),
                insufficient: Jumps(Opener.RunningJump, startX, (edgeX - 120f), (edgeX - 5f), withDoubleJump: true, withDash: true)
                    .Concat(SlideJumps(edgeX - 40f, withDoubleJump: true, withDash: true)));
        }
    }

    private async Task ProveAiLevel()
    {
        const string scene = "res://scenes/levels/Level06.tscn";
        await ProvePossible(new Section("L6 Lagerroboter", scene, p => p.GlobalPosition.X > 1450f && p.IsOnGround, 900), Hops(-100f, StandY, 360));
        await ProvePossible(new Section("L6 Scanner", scene, p => p.GlobalPosition.X > 2300f && p.IsOnGround, 600),
            Delayed([new Plan(Opener.WalkOff, 1460f, StandY, 0f, 0, null, null)], 240, 6));
        await ProvePossible(new Section("L6 Schwebekiste erreichen", scene, p => p.IsOnGround && p.GlobalPosition.Y < -230f, 400),
            Jumps(Opener.RunningJump, 1420f, 1450f, 1600f, withDoubleJump: true, withDash: false));
        await ProvePossible(new Section("L6 Scanner überspringen", scene, p => p.IsOnGround && p.GlobalPosition.X > 2170f, 400, ScannersAlwaysOn: true),
            Jumps(Opener.RunningJump, 1620f, 1640f, 1705f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = -246f }));
        await ProvePossible(new Section("L6 Drohnen", scene, p => p.GlobalPosition.X > 3720f && p.IsOnGround && p.GlobalPosition.Y < 0f, 900), Hops(2420f, StandY, 480));
        await ProvePossible(new Section("L6 Kiste 1 Paketjagd", scene, p => p.GlobalPosition.Y < -230f && p.IsOnGround, 400),
            Jumps(Opener.RunningJump, 3900f, 4050f, 4240f, withDoubleJump: true, withDash: false));
        await ProvePossible(new Section("L6 Kiste 2 Paketjagd", scene, p => p.GlobalPosition.Y < -450f && p.IsOnGround, 400),
            Jumps(Opener.RunningJump, 4260f, 4280f, 4345f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -246f }));
    }

    private async Task ProveFriendsLevel()
    {
        const string scene = "res://scenes/levels/Level07.tscn";
        await ProvePossible(new Section("L7 Präzisionskiste 1", scene, p => p.IsOnGround && p.GlobalPosition.Y < -80f && p.GlobalPosition.X < 620f, 300),
            Jumps(Opener.RunningJump, 0f, 180f, 300f, withDoubleJump: false, withDash: false).Concat(Jumps(Opener.RunningJump, 0f, 180f, 300f, withDoubleJump: true, withDash: false)));
        await ProvePossible(new Section("L7 Präzisionskiste 2", scene, p => p.IsOnGround && p.GlobalPosition.Y < -140f, 300),
            Jumps(Opener.RunningJump, 480f, 490f, 535f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -86f }));
        await ProvePossible(new Section("L7 Bewegte Kiste erreichen", scene, p => p.IsOnGround && p.GlobalPosition.X > 900f && p.GlobalPosition.X < 1450f && p.GlobalPosition.Y < -80f, 400),
            Delayed(Jumps(Opener.RunningJump, 780f, 790f, 825f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -146f }), 330, 30));
        await ProvePossible(new Section("L7 Von bewegter Kiste zum Boden", scene, p => p.IsOnGround && p.GlobalPosition.X > 1500f && p.GlobalPosition.Y < 0f, 400),
            Delayed(Jumps(Opener.RunningJump, 1000f, 1000f, 1000f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = -106f, TriggerX = -10000f }), 330, 30));
        await ProvePossible(new Section("L7 Weitsprung in Speed Jump 1", scene, p => p.IsOnGround && p.GlobalPosition.X > 2400f && p.GlobalPosition.X < 3240f, 400),
            Jumps(Opener.RunningJump, 1700f, 1980f, 2095f, withDoubleJump: true, withDash: true));
        foreach (var (name, startX, landFrom, landTo, landAbove) in new[]
        {
            ("L7 Speed Jump 1 → 2", 2430f, 3600f, 4430f, 200f),
            ("L7 Speed Jump 2 → Kiste", 3630f, 4750f, 4900f, -130f),
            ("L7 Speed Jump 3 → Boden", 5180f, 6260f, 7300f, -5f),
        })
        {
            await ProvePossible(new Section(name, scene, p => p.IsOnGround && p.GlobalPosition.X > landFrom && p.GlobalPosition.X < landTo && p.GlobalPosition.Y < landAbove, 500),
                Jumps(Opener.RunningJump, startX, startX + 700f, startX + 810f, withDoubleJump: true, withDash: true)
                    .Append(new Plan(Opener.WalkOff, startX, -110f, 0f, 0, null, null))
                    .Select(plan => plan with { StartY = -110f }));
        }

        await ProvePossible(new Section("L7 Kiste → Speed Jump 3", scene, p => p.IsOnGround && p.GlobalPosition.X > 5150f && p.GlobalPosition.X < 5990f, 400),
            Jumps(Opener.RunningJump, 4770f, 4800f, 4895f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = -146f }));
        await ProvePossible(new Section("L7 Abschussrampe → Paketplattform", scene, p => p.IsOnGround && p.GlobalPosition.Y < -430f, 400),
            [new Plan(Opener.WalkOff, 6300f, StandY, 0f, 0, null, null)]);
        await ProvePossible(new Section("L7 Luftpolster → Ziel", scene, p => p.IsOnGround && p.GlobalPosition.X > 8370f && p.GlobalPosition.Y < 0f, 700),
            Hops(7200f, -446f, 0).Append(new Plan(Opener.WalkOff, 7200f, -446f, 0f, 0, null, null)));
    }

    private async Task ProveSlideLevel()
    {
        const string scene = "res://scenes/levels/Level08.tscn";
        await ProvePossible(new Section("L8 Drop, Rutsche, Lücke, Bänder", scene, p => p.IsOnGround && p.GlobalPosition.X > 2346f && p.GlobalPosition.Y < 310f, 600),
            Jumps(Opener.RunningJump, 0f, 1200f, 1345f, withDoubleJump: true, withDash: true).Append(new Plan(Opener.WalkOff, 0f, StandY, 0f, 0, null, null)));
        await ProvePossible(new Section("L8 Rückwärtsband überspringen", scene, p => p.IsOnGround && p.GlobalPosition.X > 2960f && p.GlobalPosition.Y < 310f, 400),
            Jumps(Opener.RunningJump, 2360f, 2380f, 2500f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = 275f }));
        await ProvePossible(new Section("L8 Rückwärtsband zu Fuss", scene, p => p.IsOnGround && p.GlobalPosition.X > 2960f && p.GlobalPosition.Y < 310f, 600),
            [new Plan(Opener.WalkOff, 2360f, 275f, 0f, 0, null, null)], expectImpossible: true);
        await ProvePossible(new Section("L8 Lange Rutsche, Abschussband", scene, p => p.IsOnGround && p.GlobalPosition.X > 4900f && p.GlobalPosition.Y > 780f, 700),
            [new Plan(Opener.WalkOff, 3000f, 275f, 0f, 0, null, null)]);
        await ProvePossible(new Section("L8 Gabelung oben", scene, p => p.IsOnGround && p.GlobalPosition.X > 7600f && p.GlobalPosition.Y > 1400f, 900),
            Jumps(Opener.RunningJump, 5620f, 5700f, 5800f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = 1048f }));
        await ProvePossible(new Section("L8 Gabelung unten", scene, p => p.IsOnGround && p.GlobalPosition.X > 7600f && p.GlobalPosition.Y > 1400f, 900),
            [new Plan(Opener.WalkOff, 5620f, 1048f, 0f, 0, null, null)]);
    }

    private async Task ProveNightShiftLevel()
    {
        const string scene = "res://scenes/levels/Level09.tscn";
        await ProvePossible(new Section("L9 Erdgeschoss erste Kiste und Band", scene, p => p.IsOnGround && p.GlobalPosition.X > 1050f, 600),
            Jumps(Opener.RunningJump, -500f, 40f, 180f, withDoubleJump: false, withDash: false));
        await ProvePossible(new Section("L9 Erdgeschoss Kistenwand", scene, p => p.IsOnGround && p.GlobalPosition.X > 1440f, 600),
            Jumps(Opener.RunningJump, 1020f, 1080f, 1280f, withDoubleJump: true, withDash: false));
        await ProvePossible(new Section("L9 Erdgeschoss Rückwärtsband", scene, p => p.IsOnGround && p.GlobalPosition.X > 2210f, 600),
            Jumps(Opener.RunningJump, 1450f, 1540f, 1700f, withDoubleJump: true, withDash: true));
        await ProvePossible(new Section("L9 Erdgeschoss Kistenstapel und Rutsche", scene, p => p.IsOnGround && p.GlobalPosition.X > 3050f, 600),
            Jumps(Opener.RunningJump, 2250f, 2280f, 2480f, withDoubleJump: true, withDash: false));
        await ProvePossible(new Section("L9 Erdgeschoss in den Kamin", scene, p => p.GlobalPosition.X > 3600f && p.GlobalPosition.Y > -60f, 400),
            [new Plan(Opener.WalkOff, 3060f, StandY, 0f, 0, null, null)]);
        var chimney = new Section("L9 Breiter Kamin in den 1. Stock", scene, p => p.IsOnGround && p.GlobalPosition.Y < -700f && p.GlobalPosition.X < 3590f, 1200);
        await ProveClimb(chimney, new Vector2(3730f, StandY), floorY: 0f, useDash: false, exitAboveY: -820f, expectSuccess: true, exitDirection: -1f);
        await ProvePossible(new Section("L9 1. Stock nach links, Rolltreppe in den 2. Stock", scene, p => p.IsOnGround && p.GlobalPosition.Y < -1310f && p.GlobalPosition.X < 200f, 1500),
            Hops(3400f, -726f, 120).Select(plan => plan with { Dir = -1f, TriggerX = 100000f }));
        await ProvePossible(new Section("L9 Paket-Abstecher Kiste 1", scene, p => p.IsOnGround && p.GlobalPosition.Y < -1480f && p.GlobalPosition.X < -240f, 600),
            Jumps(Opener.RunningJump, 150f, -200f, -60f, withDoubleJump: true, withDash: false).Select(plan => plan with { Dir = -1f, StartY = -1326f, TriggerX = -plan.TriggerX - 260f }));
        await ProvePossible(new Section("L9 Paket-Abstecher Kiste 2", scene, p => p.IsOnGround && p.GlobalPosition.Y < -1680f, 600),
            Jumps(Opener.RunningJump, -330f, -320f, -260f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -1526f }));
        await ProvePossible(new Section("L9 2. Stock Lücke über der Rolltreppe", scene, p => p.IsOnGround && p.GlobalPosition.X > 430f && p.GlobalPosition.Y < -1290f, 400),
            Jumps(Opener.RunningJump, 0f, 60f, 195f, withDoubleJump: false, withDash: false).Select(plan => plan with { StartY = -1326f }));
        await ProvePossible(new Section("L9 2. Stock Band und Grube", scene, p => p.IsOnGround && p.GlobalPosition.X > 1760f && p.GlobalPosition.Y < -1290f, 600),
            Jumps(Opener.RunningJump, 450f, 1340f, 1495f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = -1326f }));
        await ProvePossible(new Section("L9 2. Stock Kistenstapel und Rutsche", scene, p => p.IsOnGround && p.GlobalPosition.X > 2480f, 600),
            Jumps(Opener.RunningJump, 1800f, 1830f, 1990f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -1326f }));
        await ProvePossible(new Section("L9 2. Stock Rückwärtsband bis Rutschen", scene, p => p.GlobalPosition.X > 4300f, 900),
            Jumps(Opener.RunningJump, 2600f, 2950f, 3100f, withDoubleJump: true, withDash: true).Select(plan => plan with { StartY = -1326f }));
        await ProvePossible(new Section("L9 Riesenrutschen bis Ziel", scene, p => p.IsOnGround && p.GlobalPosition.X > 5800f && p.GlobalPosition.Y > -40f, 900),
            [new Plan(Opener.WalkOff, 4200f, -1326f, 0f, 0, null, null)]);
    }

    private async Task ProveClimbingHallLevel()
    {
        const string scene = "res://scenes/levels/Level10.tscn";
        await ProveClimb(new Section("L10 Kamin 1 aufs Band", scene, p => p.IsOnGround && p.GlobalPosition.Y < -530f && p.GlobalPosition.X > 900f, 1200),
            new Vector2(750f, StandY), floorY: 0f, useDash: false, exitAboveY: -560f, expectSuccess: true);
        await ProveClimb(new Section("L10 Kamin 2 aufs Band", scene, p => p.IsOnGround && p.GlobalPosition.Y < -1010f && p.GlobalPosition.X > 2200f, 1200),
            new Vector2(2050f, -546f), floorY: -520f, useDash: false, exitAboveY: -1040f, expectSuccess: true);
        await ProvePossible(new Section("L10 Band in Speed Jump 1", scene, p => p.IsOnGround && p.GlobalPosition.X > 2950f && p.GlobalPosition.X < 3780f, 500),
            [new Plan(Opener.WalkOff, 2300f, -1026f, 0f, 0, null, null)]);
        foreach (var (name, startX, startY, landFrom, landTo, landAbove) in new[]
        {
            ("L10 Speed Jump 1 → 2", 2980f, -970f, 4250f, 5080f, -700f),
            ("L10 Speed Jump 2 → 3", 4280f, -870f, 5450f, 6280f, -600f),
            ("L10 Speed Jump 3 → Platte", 5480f, -770f, 6550f, 8200f, -505f),
        })
        {
            await ProvePossible(new Section(name, scene, p => p.IsOnGround && p.GlobalPosition.X > landFrom && p.GlobalPosition.X < landTo && p.GlobalPosition.Y < landAbove, 500),
                Jumps(Opener.RunningJump, startX, startX + 700f, startX + 810f, withDoubleJump: true, withDash: true)
                    .Append(new Plan(Opener.WalkOff, startX, startY, 0f, 0, null, null))
                    .Select(plan => plan with { StartY = startY }));
        }

        await ProvePossible(new Section("L10 Abschussrampe → Platte", scene, p => p.IsOnGround && p.GlobalPosition.Y < -930f, 500),
            [new Plan(Opener.WalkOff, 6700f, -526f, 0f, 0, null, null)]);
        await ProveClimb(new Section("L10 Kamin 3 aufs Band", scene, p => p.IsOnGround && p.GlobalPosition.Y < -1380f && p.GlobalPosition.X > 8200f, 1200),
            new Vector2(8050f, -946f), floorY: -920f, useDash: false, exitAboveY: -1410f, expectSuccess: true);
        await ProvePossible(new Section("L10 Band, Rutschen bis Ziel", scene, p => p.IsOnGround && p.GlobalPosition.X > 10500f && p.GlobalPosition.Y > -220f, 900),
            [new Plan(Opener.WalkOff, 8300f, -1396f, 0f, 0, null, null)]);
        await ProvePossible(new Section("L10 Paketkiste", scene, p => p.IsOnGround && p.GlobalPosition.Y < -330f, 400),
            Jumps(Opener.RunningJump, 11000f, 11150f, 11340f, withDoubleJump: true, withDash: false).Select(plan => plan with { StartY = -204f }));
    }

    private static IEnumerable<Plan> Jumps(Opener opener, float startX, float firstTrigger, float lastTrigger, bool withDoubleJump, bool withDash)
    {
        for (var trigger = firstTrigger; trigger <= lastTrigger; trigger += 20f)
        {
            foreach (var (dj, dash) in AirTimings(withDoubleJump, withDash))
            {
                yield return new Plan(opener, startX, StandY, trigger, 0, dj, dash);
            }
        }
    }

    private static IEnumerable<Plan> SlideJumps(float startX, bool withDoubleJump, bool withDash)
    {
        foreach (var back in new[] { 15, 25, 35, 45 })
        {
            foreach (var (dj, dash) in AirTimings(withDoubleJump, withDash))
            {
                yield return new Plan(Opener.SlideJump, startX, StandY, 0f, back, dj, dash);
            }
        }
    }

    private static IEnumerable<(int? DoubleJump, int? Dash)> AirTimings(bool withDoubleJump, bool withDash)
    {
        if (!withDoubleJump && !withDash)
        {
            yield return (null, null);
            yield break;
        }

        if (withDoubleJump && !withDash)
        {
            for (var dj = 6; dj <= 78; dj += 6)
            {
                yield return (dj, null);
            }

            yield break;
        }

        if (withDash && !withDoubleJump)
        {
            for (var dash = 2; dash <= 78; dash += 4)
            {
                yield return (null, dash);
            }

            yield break;
        }

        for (var first = 6; first <= 66; first += 6)
        {
            for (var second = first + 4; second <= first + 44; second += 8)
            {
                yield return (first, second);
                yield return (second, first);
            }
        }
    }

    private bool IsSelected(Section section) => _only is null || section.Name.StartsWith(_only);

    private async Task Prove(Section section, IEnumerable<Plan> required, IEnumerable<Plan> insufficient)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var requiredResults = await Run(section, required.ToList());
        var insufficientResults = await Run(section, insufficient.ToList());

        var requiredSuccesses = requiredResults.Count(r => r.Success);
        var cheats = insufficientResults.Where(r => r.Success).ToList();
        GD.Print($"{section.Name}: gefordert {requiredSuccesses}/{requiredResults.Count} geschafft, schwächer {cheats.Count}/{insufficientResults.Count} geschafft");

        if (requiredSuccesses == 0)
        {
            _failures.Add($"{section.Name}: geforderte Technik schafft es nie");
        }

        foreach (var cheat in cheats.Take(3))
        {
            _failures.Add($"{section.Name}: schwächere Technik schafft es: {cheat.Plan.Describe()}");
        }
    }

    private async Task ProveImpossible(Section section, IEnumerable<Plan> plans)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var results = await Run(section, plans.ToList());
        var cheats = results.Where(r => r.Success).ToList();
        GD.Print($"{section.Name}: {cheats.Count}/{results.Count} Varianten schaffen es");
        foreach (var cheat in cheats.Take(3))
        {
            _failures.Add($"{section.Name}: möglich mit {cheat.Plan.Describe()}");
        }
    }

    private Task ProveWallClimb(Section section) =>
        ProveClimb(section, new Vector2(2300f, StandY), floorY: 0f, useDash: false, exitAboveY: null, expectSuccess: true);

    private async Task ProveClimb(Section section, Vector2 start, float floorY, bool useDash, float? exitAboveY, bool expectSuccess, float exitDirection = 1f)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var level = LoadLevel(section.ScenePath);
        var probe = SpawnProbe(level, start);
        var input = (ScriptedInput)probe.InputSource;
        var success = false;
        var direction = 1f;
        var dashedSinceKick = false;
        var usedDoubleJump = false;
        var exiting = false;
        var doubleJumpEachKick = !useDash;
        await NextTick();
        input.MoveX = direction;
        input.PressJump();
        for (var tick = 0; tick < section.Ticks && !success; tick++)
        {
            await NextTick();
            var airborne = probe.State is PlayerState.Airborne or PlayerState.Dashing;
            if (!exiting && probe.IsTouchingWall && airborne)
            {
                direction = -direction;
                input.MoveX = direction;
                input.PressJump();
                dashedSinceKick = false;
                usedDoubleJump &= !doubleJumpEachKick;
                exiting = direction == exitDirection && exitAboveY is { } exitY && probe.GlobalPosition.Y < exitY + 80f;
            }
            else if (!exiting && airborne && probe.Velocity.Y >= -40f && !dashedSinceKick && useDash)
            {
                input.PressAction();
                dashedSinceKick = true;
            }
            else if (!exiting && airborne && probe.Velocity.Y >= 0f && !usedDoubleJump && probe.GlobalPosition.Y < floorY - 60f)
            {
                input.PressJump();
                usedDoubleJump = true;
            }
            else if (probe.State == PlayerState.Airborne && probe.Velocity.Y > 0f && probe.GlobalPosition.Y > floorY - 200f && !usedDoubleJump)
            {
                input.PressJump();
                usedDoubleJump = true;
            }

            success = section.Reached(probe);
            Trace(section, tick, probe);
        }

        var outcome = success ? "oben angekommen" : $"nicht hochgekommen (y {probe.GlobalPosition.Y:0})";
        GD.Print($"{section.Name}: Kletter-Bot {(useDash ? "mit" : "ohne")} Dash {outcome}");
        if (success != expectSuccess)
        {
            _failures.Add($"{section.Name}: Kletter-Bot {(useDash ? "mit" : "ohne")} Dash: erwartet {(expectSuccess ? "Erfolg" : "Scheitern")}, war {outcome}");
        }

        UnloadLevel(level);
    }

    private void Trace(Section section, int tick, PlayerController probe)
    {
        if (_trace && tick % 15 == 0)
        {
            GD.Print($"  {section.Name} t{tick} pos {probe.GlobalPosition.X:0},{probe.GlobalPosition.Y:0} vel {probe.Velocity.X:0},{probe.Velocity.Y:0} {probe.State} wall {probe.IsTouchingWall}");
        }
    }

    private sealed record Result(Plan Plan, bool Success, int SuccessTick);

    private async Task<List<Result>> Run(Section section, List<Plan> plans)
    {
        var level = LoadLevel(section.ScenePath);
        if (section.ScannersAlwaysOn)
        {
            foreach (var beam in level.FindChildren("*", nameof(Area2D), true, false).OfType<ScanBeam>())
            {
                beam.OffTime = 0.001f;
            }
        }

        var probes = plans.Select(plan => (Plan: plan, Player: SpawnProbe(level, new Vector2(plan.StartX, plan.StartY)), State: new ProbeState())).ToList();
        for (var tick = 0; tick < section.Ticks; tick++)
        {
            foreach (var probe in probes.Where(p => !p.State.Success && !p.State.Dead))
            {
                Drive(probe.Plan, probe.Player, probe.State, tick);
            }

            await NextTick();
            Trace(section, tick, probes[0].Player);
            foreach (var probe in probes.Where(p => !p.State.Success && !p.State.Dead))
            {
                probe.State.Dead = probe.Player.GlobalPosition.Y - StandY > section.DiesBelowY || _scanned.Contains(probe.Player);
                probe.State.Success = !probe.State.Dead && section.Reached(probe.Player);
                if (probe.State.Success)
                {
                    probe.State.SuccessTick = tick;
                }
            }
        }

        UnloadLevel(level);
        return probes.Select(p => new Result(p.Plan, p.State.Success, p.State.SuccessTick)).ToList();
    }

    private sealed class ProbeState
    {
        public bool Success { get; set; }
        public bool Dead { get; set; }
        public int SuccessTick { get; set; } = -1;
        public int TakeoffTick { get; set; } = -1;
        public int TriggerTick { get; set; } = -1;
    }

    private static void Drive(Plan plan, PlayerController player, ProbeState state, int tick)
    {
        var input = (ScriptedInput)player.InputSource;
        if (tick < plan.DelayTicks)
        {
            input.MoveX = 0f;
            return;
        }

        tick -= plan.DelayTicks;
        if (plan.Opener == Opener.Hopper)
        {
            DriveHopper(plan, player, state, input, tick);
            return;
        }

        if (state.TakeoffTick >= 0)
        {
            input.MoveX = plan.Dir;
            var flight = tick - state.TakeoffTick;
            if (flight == plan.DoubleJumpTick)
            {
                input.PressJump();
            }

            if (flight == plan.DashTick)
            {
                input.PressAction();
            }

            return;
        }

        switch (plan.Opener)
        {
            case Opener.WalkOff:
                input.MoveX = plan.Dir;
                return;
            case Opener.RunningJump:
                input.MoveX = plan.Dir;
                if (player.GlobalPosition.X * plan.Dir >= plan.TriggerX * plan.Dir && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                break;
            case Opener.SlideJump:
                input.MoveX = tick < plan.RunBackTicks ? -1f : 1f;
                if (tick == plan.RunBackTicks + 3)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                break;
            case Opener.RollJump:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressAction();
                }

                if (state.TriggerTick >= 0 && tick == state.TriggerTick + 3)
                {
                    input.PressJump();
                }

                break;
            case Opener.JumpThenRoll:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && state.TriggerTick < 0)
                {
                    state.TriggerTick = tick;
                    input.PressJump();
                }

                if (state.TriggerTick >= 0 && player.Velocity.Y > 0f && player.GlobalPosition.Y > -70f)
                {
                    input.PressAction();
                }

                return;
            case Opener.Roll:
                input.MoveX = 1f;
                if (player.GlobalPosition.X >= plan.TriggerX && player.State == PlayerState.Grounded)
                {
                    input.PressAction();
                }

                return;
        }

        if (state.TriggerTick >= 0 && player.State == PlayerState.Airborne)
        {
            state.TakeoffTick = tick;
        }
    }

    private static void DriveHopper(Plan plan, PlayerController player, ProbeState state, ScriptedInput input, int tick)
    {
        input.MoveX = plan.Dir;
        if (player.IsOnGround && player.GlobalPosition.X * plan.Dir >= plan.TriggerX * plan.Dir)
        {
            input.PressJump();
            state.TakeoffTick = tick;
            return;
        }

        if (state.TakeoffTick >= 0 && tick - state.TakeoffTick == plan.DoubleJumpTick)
        {
            input.PressJump();
        }
    }

    private static IEnumerable<Plan> Hops(float startX, float startY, int maxDelay)
    {
        for (var delay = 0; delay <= maxDelay; delay += 15)
        {
            foreach (var dj in new int?[] { null, 20, 35, 50 })
            {
                yield return new Plan(Opener.Hopper, startX, startY, startX, 0, dj, null, delay);
            }
        }
    }

    private static IEnumerable<Plan> Delayed(IEnumerable<Plan> plans, int maxDelay, int step) =>
        plans.SelectMany(plan => Enumerable.Range(0, (maxDelay / step) + 1).Select(i => plan with { DelayTicks = i * step }));

    private async Task ProvePossible(Section section, IEnumerable<Plan> plans, bool expectImpossible = false)
    {
        if (!IsSelected(section))
        {
            return;
        }

        var results = await Run(section, plans.ToList());
        var successes = results.Count(r => r.Success);
        var fastest = results.Where(r => r.Success).Select(r => r.SuccessTick).DefaultIfEmpty(-1).Min();
        var fastestText = fastest < 0 ? string.Empty : $", schnellste {fastest / (double)Engine.PhysicsTicksPerSecond:0.00}s";
        GD.Print($"{section.Name}: {successes}/{results.Count} Varianten schaffen es{fastestText}");
        if (successes == 0 && !expectImpossible)
        {
            _failures.Add($"{section.Name}: keine Variante schafft es");
        }

        if (successes > 0 && expectImpossible)
        {
            _failures.Add($"{section.Name}: sollte unmöglich sein");
        }
    }

    private Node LoadLevel(string scenePath)
    {
        var level = GD.Load<PackedScene>(scenePath).Instantiate();
        AddChild(level);
        var originalPlayer = level.GetNode<PlayerController>("Player");
        originalPlayer.ProcessMode = ProcessModeEnum.Disabled;
        originalPlayer.CollisionLayer = 0;
        originalPlayer.GlobalPosition = new Vector2(-100000f, -100000f);
        foreach (var beam in level.FindChildren("*", nameof(Area2D), true, false).OfType<ScanBeam>())
        {
            beam.PlayerScanned += player => _scanned.Add(player);
        }

        return level;
    }

    private void UnloadLevel(Node level)
    {
        RemoveChild(level);
        level.QueueFree();
    }

    private static PlayerController SpawnProbe(Node level, Vector2 position)
    {
        var player = PlayerScene.Instantiate<PlayerController>();
        foreach (var name in new[] { "Visual", "Trail", "Camera" })
        {
            var child = player.GetNode(name);
            player.RemoveChild(child);
            child.Free();
        }

        player.InputSource = new ScriptedInput();
        player.CollisionLayer = CollisionLayers.GhostPlayers;
        player.CollisionMask = CollisionLayers.World;
        player.Position = position;
        level.AddChild(player);
        return player;
    }

    private async Task NextTick() => await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

    private sealed class ScriptedInput : IPlayerInputSource
    {
        private bool _jumpPressed;
        private bool _actionPressed;

        public float MoveX { get; set; }

        public bool JumpHeld { get; private set; }

        public void PressJump()
        {
            _jumpPressed = true;
            JumpHeld = true;
        }

        public void PressAction() => _actionPressed = true;

        public PlayerInput Sample()
        {
            var sample = new PlayerInput(MoveX, _jumpPressed, JumpHeld, _actionPressed);
            _jumpPressed = false;
            _actionPressed = false;
            return sample;
        }
    }
}
