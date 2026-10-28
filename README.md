# Warehouse Runner 2D

Jump-and-Run in einer Lagerhalle: einstempeln, das goldene Express-Paket
finden, zur Rampe bringen, den LKW losschicken, ausstempeln, und das so schnell
wie möglich.

- Engine: Godot 4.7 (.NET) mit C#, siehe [ADR 0001](docs/adr/0001-engine-godot-csharp.md)
- Bewegungen, Levelablauf und ergänzte Funktionen: [docs/gameplay.md](docs/gameplay.md)

## Voraussetzungen

- Godot 4.7.2 .NET (portabel), z. B. unter `C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64`
- .NET SDK 8 oder neuer

## Starten

Im Editor:

```powershell
& "C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path C:\Projects\WarehouseRunner2D -e
```

Danach im Editor **F5**. Das Spiel startet im Menü mit der Levelauswahl (`scenes/menu/MainMenu.tscn`).

Direkt spielen, ohne Editor:

```powershell
dotnet build
& "C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path C:\Projects\WarehouseRunner2D
```

## Steuerung

| Aktion | Tastatur | Gamepad |
|---|---|---|
| Laufen | A/D, ←/→ | Linker Stick, D-Pad |
| Springen / Doppelsprung / Wall Kick | Leertaste | A |
| Dash (Luft) / Rollout (Landung) / Rolle (im Laufen) | Shift, J, Ctrl, K | X, B |
| Ducken (halten) | S, ↓ | Linker Trigger |
| Dash-Richtung (beim Dash halten) | W/S oder ↑/↓, zusammen mit A/D oder ←/→ | Linker Stick hoch/runter |
| Neustart | R | Start |
| Zurück zur Levelauswahl | Esc | Back |
| Nach dem Ziel: nächstes Level | Enter | Y |
| Hilfe ein/aus | F1 | – |
| Menü: auswählen / starten | ↑/↓ oder W/S, Enter/Leertaste | Stick/D-Pad, A |

## Tests

Bewegungstests (headless):

```powershell
$godot = "C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
dotnet build
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/MovementTests.tscn
```

Level-Ablauf in allen Levels (Einstempeln → Paket → Rampe → Ausstempeln):

```powershell
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn
```

Fähigkeits- und Machbarkeits-Beweise (dauert rund 20 Minuten): pro Hindernis der
Lern-Levels wird geprüft, dass die geforderte Technik durchkommt und schwächere
Techniken mit keinem Timing. In Level 6 und 7 wird jeder Abschnitt auf
Schaffbarkeit geprüft. Mit `-- --only=L7` nur einen Teil prüfen:

```powershell
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/LevelSkillProofs.tscn
```

Reichweiten-Tabelle aller Techniken (Hilfe für Leveldesign):

```powershell
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/MovementEnvelope.tscn
```

Mit Fenster und Screenshots jeder Station:

```powershell
& $godot --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn -- --snapshots=C:\Temp\snapshots
```

Exit-Code 0 = alles grün.

## Struktur

```
scenes/levels/     LevelNN.tscn; Reihenfolge und Pakete stehen in scripts/Core/LevelCatalog.cs
scenes/menu/       Startmenü mit Levelauswahl
scenes/player/     Spieler-Szene
scenes/tests/      Test-Szenen
scripts/Core/      Eingabe-Belegung (Autoload), Levelkatalog, Bestzeiten
scripts/Menu/      Startmenü
scripts/Player/    Steuerung, Tuning, Darstellung, Kamera
scripts/Elements/  Kisten, Boden, Förderband, Stempeluhr, Paket, Rampe, Checkpoints
scripts/Level/     Rennablauf, HUD, Hintergrund
scripts/Tests/     Bewegungs- und Ablauftests
docs/              Entscheidungen und Gameplay-Doku
```

Level-Elemente sind `[GlobalClass]`-Nodes (`Crate`, `ConcreteBlock`,
`ConveyorBelt`, `TimeClock`, `ExpressPackage`, `LoadingDock`, `Checkpoint`,
`KillZone`) und lassen sich im Editor über „Node hinzufügen“ direkt platzieren.
Eine `Crate` mit Rotation ist automatisch rutschig.
