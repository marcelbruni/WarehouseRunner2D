using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class KillZone : PlayerTrigger
{
    public KillZone()
    {
        TriggerSize = new Vector2(1000f, 100f);
    }
}
