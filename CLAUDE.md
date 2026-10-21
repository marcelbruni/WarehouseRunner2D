# CLAUDE.md

Godot 4.7.2 .NET + C# (net8.0). Godot liegt portabel unter
`C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64` (Konsolen-Exe: `..._console.exe`).

## Build & Test

- Build: `dotnet build` im Repo-Root (eigene `nuget.config`, nur nuget.org).
- Nach neuen Dateien/Ressourcen einmal importieren: `<godot> --headless --path . --import`.
- Bewegungstests: `<godot> --headless --path . --fixed-fps 120 res://scenes/tests/MovementTests.tscn` (Exit-Code 0 = grün).
- Level-Ablauf: `<godot> --headless --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn`.
- Screenshots: ohne `--headless`, mit `-- --snapshots=<dir>`. Das Fenster muss sichtbar sein, sonst sind die Bilder veraltet.

## Konventionen

- Code-Bezeichner Englisch, UI-Texte Deutsch (Schweizer Schreibweise, kein ß).
- Bewegungswerte nur in `MovementTuning`, nicht im Controller hartkodieren.
- Eingaben immer über `IPlayerInputSource` (Tests und späteres Netzwerk hängen daran).
- Jede neue Bewegung braucht ein Szenario in `MovementTests`.
