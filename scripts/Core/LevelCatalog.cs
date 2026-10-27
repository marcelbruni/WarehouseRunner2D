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
        new("Grundausbildung", "Sprünge, Rollen, Dash und Wall Kicks · ohne Rutschen und Bänder",
        [
            new("level01", "Erster Arbeitstag", "Sprung · Doppelsprung · Slide Jump · Wall Kick", "res://scenes/levels/Level01.tscn", new(7.00, 8.0, 9.5)),
            new("level02", "Rollen lernen", "Rolle · Rollout · Durchrollen unter Regalen", "res://scenes/levels/Level02.tscn", new(6.65, 7.5, 9.0)),
            new("level03", "Dash-Schulung", "Doppelsprung + Dash · Slide Jump + Doppelsprung · Sprung + Dash", "res://scenes/levels/Level03.tscn", new(7.40, 8.5, 10.0)),
            new("level04", "Profi-Tricks", "Roll Jump · Power-Dash · Dash Jump · Dash-Rollout", "res://scenes/levels/Level04.tscn", new(6.25, 7.0, 8.5)),
            new("level06", "AI Level", "Lagerroboter · Barcode-Scanner · Lieferdrohnen · ein Paket mit eigenem Willen", "res://scenes/levels/Level06.tscn", new(13.15, 15.0, 18.0)),
        ]),
        new("Förderhalle", "Rutschkisten und Förderbänder · erst nur Bänder, dann beides",
        [
            new("level07", "Friends Ideas", "Präzisionskisten · Speed-Jump-Bänder · Abschussrampe · Luftpolsterfolie", "res://scenes/levels/Level07.tscn", new(13.00, 15.0, 17.5)),
            new("level11", "Taktstrasse", "Kisten im Takt · Speed Jumps in den Kamin · Polsterfolie · schräge Polsterfolie", "res://scenes/levels/Level11.tscn", new(17.00, 19.5, 23.0)),
            new("level05", "Hauptlager", "Alles zusammen: Rutschkisten, Förderbänder, Paketsuche", "res://scenes/levels/Level05.tscn", new(14.10, 16.0, 19.0)),
            new("level08", "Rutschpartie", "Lange Rutschkisten · Förderbänder · Rückwärtsband · Gabelung oben/unten", "res://scenes/levels/Level08.tscn", new(8.90, 10.0, 12.0)),
            new("level10", "Kletterhalle", "Kamine mit Förderband-Ausstieg · Speed Jumps · Abschussrampe · lange Rutschen", "res://scenes/levels/Level10.tscn", new(19.65, 22.5, 26.5)),
            new("level09_hochregal", "Hochregallager", "Zickzack über drei Stockwerke · breiter Kamin · Rolltreppe · Riesenrutschen", "res://scenes/levels/Level09.tscn", new(28.85, 33.0, 39.0)),
        ]),
    ];

    public static IReadOnlyList<LevelInfo> Levels { get; } = Packages.SelectMany(package => package.Levels).ToList();
}
