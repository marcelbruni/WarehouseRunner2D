using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class ConcreteBlock : BoxBody
{
    private static readonly Color Concrete = new(0.42f, 0.43f, 0.45f);
    private static readonly Color ConcreteDark = new(0.3f, 0.31f, 0.33f);
    private static readonly Color SafetyLine = new(0.95f, 0.78f, 0.12f);

    public override void _Draw()
    {
        var rect = LocalRect;
        DrawRect(rect, Concrete);
        for (var x = rect.Position.X + 200f; x < rect.End.X; x += 200f)
        {
            DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), ConcreteDark, 2f);
        }

        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 5f)), SafetyLine);
    }
}
