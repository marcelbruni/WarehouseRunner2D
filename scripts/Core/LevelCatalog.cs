using System.Collections.Generic;

namespace WarehouseRunner.Core;

public sealed record LevelInfo(string Id, string Title, string Skills, string ScenePath);

public static class LevelCatalog
{
    public const string MenuScenePath = "res://scenes/menu/MainMenu.tscn";

    public static IReadOnlyList<LevelInfo> Levels { get; } =
    [
        new("level01", "Erster Arbeitstag", "Sprung · Doppelsprung · Slide Jump · Wall Kick", "res://scenes/levels/Level01.tscn"),
        new("level02", "Dash-Schulung", "Sprung + Dash · Doppelsprung + Dash · Slide Jump + Doppelsprung", "res://scenes/levels/Level02.tscn"),
        new("level03", "Rollen lernen", "Rolle · Rollout · Durchrollen unter Regalen", "res://scenes/levels/Level03.tscn"),
        new("level04", "Profi-Tricks", "Roll Jump · Power-Dash · Dash Jump · Dash-Rollout", "res://scenes/levels/Level04.tscn"),
        new("level05", "Hauptlager", "Alles zusammen: Rutschkisten, Förderbänder, Paketsuche", "res://scenes/levels/Level05.tscn"),
    ];
}
