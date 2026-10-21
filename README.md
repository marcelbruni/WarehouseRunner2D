# Warehouse Runner 2D

Jump-and-Run in einer Lagerhalle: einstempeln, das goldene Express-Paket
finden, zur Rampe bringen, den LKW losschicken, ausstempeln, und das so schnell
wie möglich. Der Schwerpunkt liegt auf einer präzisen Steuerung im Stil von
Super Mario 64.

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

Danach im Editor **F5** (Hauptszene `scenes/levels/Level01.tscn`).

Direkt spielen, ohne Editor:

```powershell
dotnet build
& "C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path C:\Projects\WarehouseRunner2D
```

## Steuerung

| Aktion | Tastatur | Gamepad |
|---|---|---|
| Laufen | A/D, ←/→ | Linker Stick, D-Pad |
| Springen / Doppelsprung / Wall Kick | Leertaste, W, ↑ | A |
| Dash (in der Luft) | Shift, J | X |
| Rollout (bei der Landung) | S, ↓, Ctrl, K | B |
| Neustart | R | Start |
| Hilfe ein/aus | F1 | Back |

## Tests

Bewegungstests (16 Szenarien, headless):

```powershell
$godot = "C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
dotnet build
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/MovementTests.tscn
```

Level-Ablauf (Einstempeln → Paket → Rampe → Ausstempeln):

```powershell
& $godot --headless --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn
```

Mit Fenster und Screenshots jeder Station:

```powershell
& $godot --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn -- --snapshots=C:\Temp\snapshots
```

Exit-Code 0 = alles grün.

## Struktur

```
scenes/levels/     Level (Level01.tscn)
scenes/player/     Spieler-Szene
scenes/tests/      Test-Szenen
scripts/Core/      Eingabe-Belegung (Autoload)
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
