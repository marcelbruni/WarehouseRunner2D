using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class Crate : BoxBody
{
    // One crate stack step. Heights read as moves: 1 = jump, 2 = double jump,
    // 3 = slide jump + double jump, 4+ = wall kick (see MovementEnvelope).
    public const float GridSize = 96f;

    private const float SlipperyMinTiltDegrees = 3f;
    private const int MaxJumpableCells = 4;

    private static readonly Color Wood = new(0.69f, 0.48f, 0.24f);
    private static readonly Color WoodDark = new(0.43f, 0.29f, 0.13f);
    private static readonly Color Film = new(0.75f, 0.9f, 1f, 0.35f);
    private static readonly Color FilmShine = new(1f, 1f, 1f, 0.55f);

    [Export]
    public bool AlwaysSlippery { get; set; }

    public bool IsSlippery => AlwaysSlippery || Mathf.Abs(Mathf.PosMod(RotationDegrees + 45f, 90f) - 45f) > SlipperyMinTiltDegrees;

    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged)
        {
            QueueRedraw();
            if (Engine.IsEditorHint())
            {
                UpdateConfigurationWarnings();
            }
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        // Thin ledges, slopes and tall wall-kick columns are exempt; only floor
        // obstacles a player has to judge by eye need to sit on the grid.
        var height = Size.Y;
        var isObstacle = !IsSlippery
            && Size.X >= GridSize * 0.9f
            && height > GridSize * 0.6f
            && height < GridSize * (MaxJumpableCells + 0.5f);
        var cells = height / GridSize;
        if (isObstacle && Mathf.Abs(cells - Mathf.Round(cells)) > 0.01f)
        {
            return [$"Height {height:0} is off the crate grid ({GridSize:0} px). Nearest grid height: {Mathf.Max(1f, Mathf.Round(cells)) * GridSize:0}."];
        }

        return [];
    }

    public override void _EnterTree()
    {
        SetNotifyTransform(true);
    }

    public override void _Draw()
    {
        var rect = LocalRect;
        var columns = Mathf.Max(1, Mathf.RoundToInt(rect.Size.X / GridSize));
        var rows = Mathf.Max(1, Mathf.RoundToInt(rect.Size.Y / GridSize));
        var cell = new Vector2(rect.Size.X / columns, rect.Size.Y / rows);

        for (var column = 0; column < columns; column++)
        {
            for (var row = 0; row < rows; row++)
            {
                DrawCrateCell(new Rect2(rect.Position + (cell * new Vector2(column, row)), cell));
            }
        }

        if (IsSlippery)
        {
            DrawStretchFilm(rect);
        }
    }

    private void DrawCrateCell(Rect2 cell)
    {
        var border = Mathf.Clamp(Mathf.Min(cell.Size.X, cell.Size.Y) * 0.12f, 3f, 8f);
        DrawRect(cell, Wood);
        DrawRect(cell, WoodDark, false, border);
        var inner = cell.Grow(-border);
        DrawLine(inner.Position, inner.End, WoodDark, border * 0.8f);
        DrawLine(new Vector2(inner.Position.X, inner.End.Y), new Vector2(inner.End.X, inner.Position.Y), WoodDark, border * 0.8f);
    }

    private void DrawStretchFilm(Rect2 rect)
    {
        DrawRect(rect, Film);
        for (var x = rect.Position.X + 10f; x < rect.End.X - 14f; x += 34f)
        {
            DrawLine(new Vector2(x, rect.Position.Y + 3f), new Vector2(x + 14f, rect.Position.Y + 3f), FilmShine, 3f);
        }

        DrawRect(rect, FilmShine, false, 2f);
    }
}
