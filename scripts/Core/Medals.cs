using System;
using Godot;

namespace WarehouseRunner.Core;

public enum Medal
{
    None,
    Bronze,
    Silver,
    Gold,
}

// Gold is the developer's own best time; silver and bronze are roughly +15 % and +35 %,
// rounded to half seconds so they are easy to remember.
public sealed record MedalTimes(double Gold, double Silver, double Bronze)
{
    public Medal For(double seconds)
    {
        // Compare what the timer shows (hundredths), so 00:07.00 on screen is really gold.
        var shown = Math.Round(seconds, 2, MidpointRounding.AwayFromZero);
        return shown <= Gold ? Medal.Gold
            : shown <= Silver ? Medal.Silver
            : shown <= Bronze ? Medal.Bronze
            : Medal.None;
    }

    public double TimeFor(Medal medal) => medal switch
    {
        Medal.Gold => Gold,
        Medal.Silver => Silver,
        Medal.Bronze => Bronze,
        _ => throw new ArgumentOutOfRangeException(nameof(medal)),
    };

    public static Medal? NextAfter(Medal medal) => medal == Medal.Gold ? null : medal + 1;

    public static string Name(Medal medal) => medal switch
    {
        Medal.Gold => "Gold",
        Medal.Silver => "Silber",
        Medal.Bronze => "Bronze",
        _ => "Keine",
    };

    public static Color ColorOf(Medal medal) => medal switch
    {
        Medal.Gold => new Color(1f, 0.82f, 0.1f),
        Medal.Silver => new Color(0.82f, 0.85f, 0.9f),
        Medal.Bronze => new Color(0.85f, 0.52f, 0.25f),
        _ => new Color(0.35f, 0.37f, 0.42f),
    };
}
