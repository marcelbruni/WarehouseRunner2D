# Gameplay

## Ablauf eines Levels

1. **Einstempeln** an der Stempeluhr „EIN“. Die Lichtschranke reicht bis unter
   die Decke, man kann sie nicht überspringen. Erst ab hier läuft die Zeit.
2. **Goldenes Express-Paket finden.** Es liegt abseits des direkten Wegs (in
   Level 1 oben auf dem rechten Hochregal). Vor dem Einstempeln lässt es sich
   nicht aufnehmen.
3. **Paket zur Rampe 3 bringen.** Das Paket fliegt in den LKW, die Türen
   schliessen, der LKW fährt ab, das Rolltor geht zu.
4. **Ausstempeln** an der Stempeluhr „AUS“. Die Zeit steht, die Bestzeit wird
   gespeichert.

Wer ohne Paket an der Rampe oder zu früh an der Ausstempel-Uhr ankommt, bekommt
einen Hinweis und muss zurück.

## Bewegungen

| Bewegung | Eingabe | Wirkung |
|---|---|---|
| Laufen | A/D, Pfeile, Stick | Beschleunigt auf Laufgeschwindigkeit |
| Jump | Leertaste/W/↑, Pad A | Normaler Sprung |
| Double Jump | Jump in der Luft | Zweiter Sprung, einmal pro Luftphase |
| Jump → Dash | Aktionstaste (Shift/J/S/↓/Ctrl/K, Pad X/B) in der Luft | Horizontaler Schub ohne Schwerkraft, einmal pro Luftphase |
| Jump → Dash → Jump | Jump während/nach dem Dash | Doppelsprung, der den Dash-Schwung mitnimmt |
| Rollout nach Jump-Landung | Aktionstaste kurz vor/bei/nach der Landung | Rolle + Boost (×1.3 für 0.8 s) |
| Rollout nach Dash-Landung | wie oben, nach einem Dash | Stärkerer Boost (×1.45 für 1.0 s) |
| Rolle | Aktionstaste am Boden, nur im Laufen | Etwas langsamer als der Rollout (×1.2 für 0.6 s); erneut erst, wenn das Tempo wieder normal ist |
| Roll Jump | Jump direkt aus Rolle oder Rollout | Fliegt weiter: horizontal ×1.2 (max. 650 px/s) |
| Power-Dash | Dash nach einem Roll Jump | Stärkerer Dash (860 statt 720, danach 620 statt 520 px/s) |
| Slide Jump | Rennen, abrupt Gegenrichtung (Rutsch-Pose), Jump; die Rutsch-Pose bleibt auch beim Zurück-und-wieder-vor | Etwas höher als normaler Sprung (+31 %), springt in die aktuell gedrückte Richtung |
| Slide Jump → Double Jump / Dash / Dash → Jump | wie oben kombinieren | Alle Ketten möglich |
| Schräge Kiste | einfach draufstehen | Rutschen bergab, Boost (×1.35) während und 0.9 s danach |
| Förderband | draufstehen/laufen | Bandgeschwindigkeit wird addiert (schneller oder langsamer); beim Absprung bleibt der Schwung |

**Aktionstaste (Dash und Rollout auf derselben Taste):**

- Kurz nach der Landung: Rollout. Am Boden im Laufen: Rolle. Im Stand: nichts.
- In der Luft, Boden noch weit weg: Dash.
- In der Luft, Landung in weniger als 0.15 s: Rollout (vorgemerkt). Ein Dash
  knapp über dem Boden ist deshalb nicht möglich.

**Tempo halten:** Beide Loops halten konstant ~590 px/s (≈1.7× Laufen),
ohne sich aufzuschaukeln:

- Rolle → Jump → Dash → (Landung) Rollout → Jump → Dash → …
- Jump → Dash → Rollout → Jump → Dash → …

Alle Werte stehen in `scripts/Player/MovementTuning.cs` und lassen sich im
Godot-Inspector am Player (Property `Tuning`) live ändern.

## Selbst ergänzte Funktionen

Diese Punkte waren nicht in der Vorgabe und sind von Claude hinzugefügt:

- **Wall Slide + Wall Kick:** In der Luft gegen eine Wand drücken bremst den
  Fall. Jump an der Wand stösst schräg nach oben ab und lädt den Dash wieder
  auf. Ermöglicht den „Kamin“ zwischen den beiden Hochregalen in Level 1.
- **Perfekter Rollout:** Rollout innerhalb von 0.05 s um die Landung herum gibt
  +0.1 auf den Boost-Faktor („PERFEKT!“).
- **Coyote Time (0.1 s):** Kurz nach dem Verlassen einer Kante ist noch ein
  normaler Sprung möglich.
- **Jump-Buffer (0.12 s) und Roll-Buffer (0.15 s):** Tasten, die kurz vor der
  Landung gedrückt werden, zählen trotzdem.
- **Variable Sprunghöhe:** Jump früh loslassen gibt einen kurzen Hüpfer.
- **Momentum-Erhalt:** Geschwindigkeit über dem Limit (nach Boosts, Dash,
  Förderband) wird nicht abgeschnitten, sondern baut sich langsam ab.
- **Rutschige Schrägen sind nicht begehbar:** Bergauf rutscht man zurück, man
  muss drüberspringen.
- **Schräge Förderbänder sind nicht rutschig** (nur schräge Kisten).
- **Checkpoints + Gruben:** Wer in eine Grube fällt, startet am letzten
  Checkpoint neu. Die Zeit läuft weiter, das Paket bleibt in der Hand.
- **Bestzeit** pro Level (gespeichert in `user://records.cfg`).
- **HUD:** Timer, Ziel-Anzeige, Tempo-/Boost-Balken, Trick-Einblendungen,
  Hinweise, Hilfe (F1).
- **Feedback:** Squash & Stretch, Salto-Animationen, Staub beim Rutschen,
  Funken und Nachbilder bei Boost/Dash.
- **Zwei Stempeluhren statt einer:** Der Kurs ist linear (Start links, Ende
  rechts). Das vermeidet im späteren Multiplayer Gegenverkehr.

## Ideen für später

- Ground Pound (Stampfer) und Long Jump (wie Mario 64).
- Zerbrechliche Kisten, fahrende Gabelstapler als bewegliche Plattformen,
  Hubwagen, Rolltore als Timing-Hindernisse.
- Sound (Stempeluhr „KA-CHUNK“, Förderband-Brummen, Schritte).
- Ghost-Replay der Bestzeit (gute Vorstufe für Multiplayer).
- Mehrere Level, Level-Auswahl, Medaillen-Zeiten.
- Multiplayer: Godot High-Level Multiplayer, Server-autoritativ, Eingaben über
  `IPlayerInputSource` übertragen.
