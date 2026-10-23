namespace WarehouseRunner.Core;

public static class CollisionLayers
{
    public const uint World = 1;
    public const uint GhostPlayers = 2;
    public const uint AnyPlayer = World | GhostPlayers;
}
