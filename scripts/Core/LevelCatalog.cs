using System.Collections.Generic;

namespace WarehouseRunner.Core;

public sealed record LevelInfo(string Id, string Title, string Skills, string ScenePath);

public static class LevelCatalog
{
    public const string MenuScenePath = "res://scenes/menu/MainMenu.tscn";

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

    public static IReadOnlyList<LevelInfo> Levels { get; } =
    [
        new("level01", "Erster Arbeitstag", "Sprung · Doppelsprung · Slide Jump · Wall Kick", "res://scenes/levels/Level01.tscn"),
        new("level02", "Rollen lernen", "Rolle · Rollout · Durchrollen unter Regalen", "res://scenes/levels/Level02.tscn"),
        new("level03", "Dash-Schulung", "Doppelsprung + Dash · Slide Jump + Doppelsprung · Sprung + Dash", "res://scenes/levels/Level03.tscn"),
        new("level04", "Profi-Tricks", "Roll Jump · Power-Dash · Dash Jump · Dash-Rollout", "res://scenes/levels/Level04.tscn"),
        new("level05", "Hauptlager", "Alles zusammen: Rutschkisten, Förderbänder, Paketsuche", "res://scenes/levels/Level05.tscn"),
        new("level06", "AI Level", "Lagerroboter · Barcode-Scanner · Lieferdrohnen · ein Paket mit eigenem Willen", "res://scenes/levels/Level06.tscn"),
        new("level07", "Friends Ideas", "Präzisionskisten · Speed-Jump-Bänder · Abschussrampe · Luftpolsterfolie", "res://scenes/levels/Level07.tscn"),
        new("level08", "Rutschpartie", "Lange Rutschkisten · Förderbänder · Rückwärtsband · Gabelung oben/unten", "res://scenes/levels/Level08.tscn"),
        new("level09_hochregal", "Hochregallager", "Zickzack über drei Stockwerke · breiter Kamin · Rolltreppe · Riesenrutschen", "res://scenes/levels/Level09.tscn"),
        new("level10", "Kletterhalle", "Kamine mit Förderband-Ausstieg · Speed Jumps · Abschussrampe · lange Rutschen", "res://scenes/levels/Level10.tscn"),
    ];
}
