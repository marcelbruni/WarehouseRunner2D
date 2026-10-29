using System.Collections.Generic;
using Godot;

namespace WarehouseRunner.Core;

public partial class MusicPlayer : Node
{
    public const string MenuTheme = "res://assets/music/menu_theme.ogg";
    public const string MusicBus = "Music";
    public const string EffectsBus = "Effects";

    private const float VolumeDb = -6f;
    private const string SettingsPath = "user://settings.cfg";
    private const string AudioSection = "audio";
    private static readonly Dictionary<string, string> MutedKeys = new() { [MusicBus] = "muted", [EffectsBus] = "effects_muted" };

    private static MusicPlayer? _instance;

    private readonly AudioStreamPlayer _player = new() { VolumeDb = VolumeDb, Bus = MusicBus };
    private string? _currentPath;

    public override void _EnterTree()
    {
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
        var settings = new ConfigFile();
        settings.Load(SettingsPath);
        foreach (var (bus, key) in MutedKeys)
        {
            EnsureBus(bus);
            AudioServer.SetBusMute(AudioServer.GetBusIndex(bus), settings.GetValue(AudioSection, key, false).AsBool());
        }

        AddChild(_player);
    }

    private static void EnsureBus(string bus)
    {
        if (AudioServer.GetBusIndex(bus) >= 0)
        {
            return;
        }

        AudioServer.AddBus();
        var index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, bus);
        AudioServer.SetBusSend(index, "Master");
    }

    public override void _Notification(int what)
    {
        if (what is (int)NotificationApplicationFocusOut or (int)NotificationApplicationFocusIn)
        {
            _player.StreamPaused = what == NotificationApplicationFocusOut;
        }
    }

    public static bool IsMuted(string bus) => AudioServer.IsBusMute(AudioServer.GetBusIndex(bus));

    public static void SetMuted(string bus, bool muted)
    {
        AudioServer.SetBusMute(AudioServer.GetBusIndex(bus), muted);
        var settings = new ConfigFile();
        settings.Load(SettingsPath);
        settings.SetValue(AudioSection, MutedKeys[bus], muted);
        settings.Save(SettingsPath);
    }

    public static void Play(string path) => _instance?.Switch(path);

    public static string LevelTrack(string scenePath) => $"res://assets/music/{scenePath.GetFile().GetBaseName().ToLowerInvariant()}.ogg";

    private void Switch(string path)
    {
        if (_currentPath == path && _player.Playing)
        {
            return;
        }

        _currentPath = path;
        _player.Stream = GD.Load<AudioStream>(path);
        _player.Play();
    }
}
