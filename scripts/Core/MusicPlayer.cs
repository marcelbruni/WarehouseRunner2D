using Godot;

namespace WarehouseRunner.Core;

public partial class MusicPlayer : Node
{
    public const string MenuTheme = "res://assets/music/menu_theme.ogg";

    private const float VolumeDb = -6f;

    private static MusicPlayer? _instance;

    private readonly AudioStreamPlayer _player = new() { VolumeDb = VolumeDb };
    private string? _currentPath;

    public override void _EnterTree()
    {
        _instance = this;
        ProcessMode = ProcessModeEnum.Always;
        AddChild(_player);
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
