using Godot;

namespace WarehouseRunner.Elements;

[Tool]
[GlobalClass]
public partial class HintSign : Node2D
{
    private const int FontSize = 18;
    private const float Padding = 10f;
    private const float PostHeight = 150f;

    private static readonly Color Board = new(0.13f, 0.35f, 0.2f);
    private static readonly Color BoardEdge = new(0.9f, 0.9f, 0.85f);
    private static readonly Color Post = new(0.55f, 0.57f, 0.6f);

    private string _text = "Hinweis";

    public HintSign()
    {
        ZIndex = -1;
    }

    [Export(PropertyHint.MultilineText)]
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var textSize = font.GetMultilineStringSize(_text, HorizontalAlignment.Center, -1, FontSize);
        var board = new Rect2(
            -textSize.X / 2f - Padding,
            -PostHeight - textSize.Y - (Padding * 2f),
            textSize.X + (Padding * 2f),
            textSize.Y + (Padding * 2f));

        DrawLine(Vector2.Zero, new Vector2(0f, -PostHeight), Post, 5f);
        DrawRect(board, Board);
        DrawRect(board, BoardEdge, false, 3f);
        DrawMultilineString(
            font,
            new Vector2(board.Position.X + Padding, board.Position.Y + Padding + font.GetAscent(FontSize)),
            _text,
            HorizontalAlignment.Center,
            textSize.X,
            FontSize,
            -1,
            Colors.White);
    }
}
