using Raylib_cs;

namespace CardPathways.Rendering;

public static class Layout
{
    public const int WindowWidth = 1280;
    public const int WindowHeight = 720;

    public const int MapStartX = 50;
    public const int MapStartY = 50;

    public const int SubCellSize = 12;
    public const int CellSize = SubCellSize * 9;
    public const int CellPadding = 5;

    public const int HandStartX = 800;
    public const int HandStartY = 50;
    public const int HandCardSpacing = 140;

    public static readonly Color BackgroundColor = new Color(20, 20, 30, 255);
    public static readonly Color GridColor = new Color(40, 40, 50, 255);

    public static readonly Color ColorBlocked = new Color(30, 30, 40, 255);
    public static readonly Color ColorPassable = new Color(80, 80, 90, 255);
    public static readonly Color ColorHole = new Color(10, 10, 15, 255);
    public static readonly Color ColorDoor = new Color(200, 100, 100, 255);
    public static readonly Color ColorKey = new Color(200, 200, 100, 255);
    public static readonly Color ColorShuffle = new Color(100, 100, 200, 255);

    public static readonly Color ColorReachable = new Color(100, 200, 100, 150);
    public static readonly Color ColorHighlight = new Color(255, 255, 255, 100);
}
