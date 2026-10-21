# ADR 0001: Engine – Godot 4 mit C#

- Status: angenommen
- Datum: 2026-10-21

## Kontext

Warehouse Runner ist ein Jump-and-Run mit Fokus auf präzise, Mario-64-artige
Steuerung. Der Fahrplan:

1. 2D-Version bauen, verbessern, ausarbeiten.
2. Danach eine 3D-Version.
3. Final: mehrere Spieler rennen online gegeneinander.

Anforderungen an die Technik: einfach, möglichst performant, Sprache vertraut
(C# oder Python, Unity-Erfahrung vorhanden). Zusätzlich wird ein grosser Teil
des Codes von einem KI-Assistenten geschrieben, der den Editor nicht bedienen
kann. Szenen und Level müssen deshalb als Text bearbeitbar sein.

## Entscheidung

**Godot 4.7 (.NET-Variante) mit C#.**

- Szenen (`.tscn`) und Ressourcen sind lesbare Textdateien: Level lassen sich im
  Editor *und* per Code/Diff bearbeiten und sauber reviewen.
- 2D und 3D in derselben Engine. Physik-Grundlage für 2D (`CharacterBody2D`)
  und 3D (`CharacterBody3D`) ist fast identisch, die Steuerungslogik lässt sich
  übertragen.
- High-Level-Multiplayer ist eingebaut (ENet, WebSocket, WebRTC,
  `MultiplayerSynchronizer`/`MultiplayerSpawner`).
- Portabel, ~120 MB, keine Lizenzkosten, keine Installation, kein Account.
- Headless-Modus: Tests laufen ohne Fenster per Kommandozeile
  (`--headless --fixed-fps`), was Bewegungs-Regressionstests ermöglicht.

## Betrachtete Alternativen

### Unity (C#)

- Vorteile: bereits Erfahrung vorhanden, riesiges Ökosystem/Asset Store,
  Netcode for GameObjects bzw. Fish-Net/Mirror für Multiplayer, sehr gute
  3D-Pipeline.
- Nachteile: Szenen/Prefabs hängen am Editor (YAML mit GUIDs), ohne Editor kaum
  sinnvoll bearbeitbar. Mehrere GB Installation, Unity Hub und Account,
  Lizenzbedingungen. Längere Iterationszeiten (Domain Reload).
- **Rückfall-Option:** gut geeignet, falls Godot nicht gefällt. Die
  Bewegungslogik (`PlayerController`) ist reines C# mit wenigen
  Engine-Aufrufen (`MoveAndSlide`, `IsOnFloor`, `GetFloorNormal`) und lässt
  sich auf einen Unity `CharacterController` oder einen eigenen
  Kinematic-Controller übertragen. Level müssten neu aufgebaut werden.

### MonoGame (C#)

- Vorteile: reiner Code, sehr schlank und performant, volle Kontrolle.
- Nachteile: keine Physik, kein Editor, kein Netzwerk-Layer, 3D-Rendering
  (Modelle, Licht, Schatten) komplett selbst bauen. Der 3D-Schritt wäre ein
  Mehrfaches an Aufwand.
- **Rückfall-Option:** nur sinnvoll, wenn es bei 2D bleibt.

### Python + pygame / arcade

- Vorteile: schnellster Einstieg, sehr einfacher Code.
- Nachteile: Performance-Grenzen bei vielen Objekten/Partikeln, kein
  brauchbarer Weg zu 3D, Multiplayer komplett selbst bauen, Distribution
  (Packaging) mühsam.
- **Rückfall-Option:** nur für Wegwerf-Prototypen.

### Nicht näher betrachtet

- Unreal Engine: für ein kleines 2D-Projekt zu schwer, C++/Blueprints statt C#.
- Stride (C#): 3D-stark, aber kleine Community und schwaches 2D.

## Konsequenzen

- Godot liegt portabel unter `C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64`
  (nicht im Repo).
- Das Projekt baut mit dem normalen .NET SDK (`dotnet build`), Target `net8.0`.
- Eigene `nuget.config` im Repo, damit nur nuget.org verwendet wird.
- Eingaben laufen über `IPlayerInputSource`. Tests und später das Netzwerk
  speisen dieselbe Steuerung, ohne den Controller zu ändern.
- Physik läuft mit 120 Ticks/s und Physics Interpolation. Das ist eine gute
  Basis für eine deterministische(re) Simulation im Multiplayer.
