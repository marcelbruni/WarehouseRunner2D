using System.Collections.Generic;
using System.Linq;

namespace WarehouseRunner.Core;

public sealed record LevelInfo(string Id, string Title, string Skills, string ScenePath, MedalTimes Medals);

public sealed record LevelPackage(string Title, string Description, IReadOnlyList<LevelInfo> Levels);

public static class LevelCatalog
{
    public const string MenuScenePath = "res://scenes/menu/MainMenu.tscn";

    public static LevelInfo? Find(string levelId)
    {
        foreach (var level in Levels)
        {
            if (level.Id == levelId)
            {
                return level;
            }
        }

        return null;
    }

    public static LevelInfo? NextAfter(string levelId)
    {
        for (var i = 0; i < Levels.Count - 1; i++)
        {
            if (Levels[i].Id == levelId)
            {
                return Levels[i + 1];
            }
        }

        return null;
    }

    public static IReadOnlyList<LevelPackage> Packages { get; } =
    [
        new("Basic Training", "Jumps, rolls, dashes and wall kicks",
        [
            new("level12", "Speed Training", "Roll · Roll Jump · Power Dash · Rollout · no obstacles, just speed", "res://scenes/levels/Level12.tscn", new(3.80, 4.5, 5.0)),
            new("level01", "First Day at Work", "Jump · Double Jump · Slide Jump · Wall Kick", "res://scenes/levels/Level01.tscn", new(7.00, 8.0, 9.5)),
            new("level02", "Learning to Roll", "Roll · Rollout · rolling under shelves", "res://scenes/levels/Level02.tscn", new(6.65, 7.5, 9.0)),
            new("level03", "Dash Training", "Double Jump + Dash · Slide Jump + Double Jump · Jump + Dash", "res://scenes/levels/Level03.tscn", new(7.40, 8.5, 10.0)),
            new("level04", "Pro Tricks", "Roll Jump · Power Dash · Dash Jump · Dash Rollout", "res://scenes/levels/Level04.tscn", new(6.25, 7.0, 8.5)),
            new("level13", "Top Shelf", "Slide Jump · Double Jump · upward Dash · or a Wall Kick", "res://scenes/levels/Level13.tscn", new(3.20, 3.5, 4.5)),
            new("level06", "AI Level", "Warehouse robots · barcode scanners · delivery drones · a package with a mind of its own", "res://scenes/levels/Level06.tscn", new(13.15, 15.0, 18.0)),
            new("level14", "Forklift Hall", "Slides · forklifts · drones · rolling through gaps · Double Jump + diagonal Dash", "res://scenes/levels/Level14.tscn", new(18.00, 20.5, 24.5)),
        ]),
        new("Conveyor Hall", "Slides and conveyor belts · belts first, then both",
        [
            new("level07", "Friends Ideas", "Precision crates · speed jump belts · launch ramp · bubble wrap", "res://scenes/levels/Level07.tscn", new(13.00, 15.0, 17.5)),
            new("level11", "Rhythm Line", "Crates in rhythm · speed jumps into chimneys · bubble wrap · tilted bubble wrap", "res://scenes/levels/Level11.tscn", new(17.00, 19.5, 23.0)),
            new("level05", "Main Warehouse", "Everything together: slides, conveyor belts, package hunt", "res://scenes/levels/Level05.tscn", new(14.10, 16.0, 19.0)),
            new("level08", "Slip 'n' Slide", "Long slides · conveyor belts · reverse belt · upper/lower fork", "res://scenes/levels/Level08.tscn", new(8.90, 10.0, 12.0)),
            new("level10", "Climbing Hall", "Chimneys with conveyor exits · speed jumps · launch ramp · long slides", "res://scenes/levels/Level10.tscn", new(19.65, 22.5, 26.5)),
            new("level09_hochregal", "High-Bay Warehouse", "Zigzag over three floors · wide chimney · escalator · giant slides", "res://scenes/levels/Level09.tscn", new(28.85, 33.0, 39.0)),
        ]),
    ];

    public static IReadOnlyList<LevelInfo> Levels { get; } = Packages.SelectMany(package => package.Levels).ToList();
}
