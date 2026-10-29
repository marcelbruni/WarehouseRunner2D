# CLAUDE.md

Godot 4.7.2 .NET + C# (net8.0). Godot liegt portabel unter
`C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64` (Konsolen-Exe: `..._console.exe`).

## Build & Test

- Build: `dotnet build` im Repo-Root (eigene `nuget.config`, nur nuget.org).
- Nach neuen Dateien/Ressourcen einmal importieren: `<godot> --headless --path . --import`.
- Bewegungstests: `<godot> --headless --path . --fixed-fps 120 res://scenes/tests/MovementTests.tscn` (Exit-Code 0 = grün).
- Level-Ablauf: `<godot> --headless --path . --fixed-fps 120 res://scenes/tests/LevelFlowTest.tscn`.
- Screenshots: ohne `--headless`, mit `-- --snapshots=<dir>`. Das Fenster muss sichtbar sein, sonst sind die Bilder veraltet.

## Generatoren (`tools/`)

- Level: `python tools/levels/make_levels.py level15` schreibt `scenes/levels/Level15.tscn` neu.
  Mehrere Level als weitere Argumente; mit `LEVELS_OUT=<dir>` in einen anderen Ordner (zum Vergleichen).
  Level05 ist nicht im Generator, nur von Hand gepflegt.
- Musik: venv mit `tools/music/requirements.txt`, dann `python tools/music/make_modern_music.py level15`
  (ohne Argument alle Songs). Danach `--import` und in der `.ogg.import` `loop=true` setzen.
- Soundeffekte: `python tools/sfx/make_sfx.py [name ...]` schreibt `assets/sfx/*.wav` (nur numpy nötig).

## Konventionen

- Code-Bezeichner und UI-Texte Englisch. Die Doku (`docs/`) bleibt Deutsch (Schweizer Schreibweise, kein ß).
- Bewegungswerte nur in `MovementTuning`, nicht im Controller hartkodieren.
- Eingaben immer über `IPlayerInputSource` (Tests und späteres Netzwerk hängen daran).
- Jede neue Bewegung braucht ein Szenario in `MovementTests`.
