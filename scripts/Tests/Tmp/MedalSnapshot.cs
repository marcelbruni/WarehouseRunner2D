using Godot;
using WarehouseRunner.Core;
using WarehouseRunner.Level;

namespace WarehouseRunner.Tests;

public partial class MedalSnapshot : Node2D
{
    public override async void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        var hud = new Hud();
        AddChild(hud);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        hud.ShowResult(6.5, 7.0, true, new MedalTimes(7.0, 8.0, 9.5));
        foreach (var (seconds, file) in new[] { (0.42, args[0]), (1.5, args[1]) })
        {
            await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
            GetViewport().GetTexture().GetImage().SavePng(file);
        }

        var tree = GetTree();
        tree.ChangeSceneToFile("res://scenes/menu/MainMenu.tscn");
        await ToSignal(tree.CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        tree.Root.GetTexture().GetImage().SavePng(args[2]);
        tree.Quit();
    }
}
