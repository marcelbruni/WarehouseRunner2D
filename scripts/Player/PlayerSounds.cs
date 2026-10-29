using System.Collections.Generic;
using Godot;
using WarehouseRunner.Core;

namespace WarehouseRunner.Player;

public partial class PlayerSounds : Node
{
    private const float VolumeDb = -10f;
    private const float LandingMinImpact = 450f;
    private const float LandingLoudImpact = 1100f;
    private const float PitchVariation = 0.05f;

    private readonly Dictionary<string, AudioStreamPlayer> _players = new();

    public override void _Ready()
    {
        var player = GetParent<PlayerController>();
        player.TrickPerformed += OnTrick;
        player.Landed += OnLanded;
    }

    private void OnTrick(Trick trick, bool perfect)
    {
        var sound = trick switch
        {
            Trick.Jump or Trick.RollJump or Trick.DashJump => "jump",
            Trick.DoubleJump => "double_jump",
            Trick.SlideJump => "slide_jump",
            Trick.Dash or Trick.PowerDash => "dash",
            Trick.WallKick => "wall_kick",
            Trick.GroundRoll or Trick.JumpRollout or Trick.DashRollout => "roll",
            _ => null,
        };

        if (sound != null)
        {
            Play(sound);
        }

        if (perfect)
        {
            Play("perfect", -4f);
        }
    }

    private void OnLanded(float impact)
    {
        if (impact < LandingMinImpact)
        {
            return;
        }

        var loudness = Mathf.Clamp((impact - LandingMinImpact) / (LandingLoudImpact - LandingMinImpact), 0f, 1f);
        Play("land", Mathf.Lerp(-12f, 0f, loudness));
    }

    private void Play(string sound, float extraDb = 0f)
    {
        if (!_players.TryGetValue(sound, out var audio))
        {
            audio = new AudioStreamPlayer { Stream = GD.Load<AudioStream>($"res://assets/sfx/{sound}.wav"), MaxPolyphony = 3, Bus = MusicPlayer.EffectsBus };
            AddChild(audio);
            _players[sound] = audio;
        }

        audio.VolumeDb = VolumeDb + extraDb;
        audio.PitchScale = 1f + (float)GD.RandRange(-PitchVariation, PitchVariation);
        audio.Play();
    }
}
