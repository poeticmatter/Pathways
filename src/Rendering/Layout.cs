using Raylib_cs;

namespace CardPathways.Rendering;

public static class Layout
{
    public const int WindowWidth = 1920;
    public const int WindowHeight = 1080;

    public const int MapStartX = 50;
    public const int MapStartY = 50;

    public const int SubCellSize = 20;
    public const int CellSize = SubCellSize * 9;
    public const int CellPadding = 0;

    public const int MapCols = 5;
    public const int HandStartX = MapStartX + MapCols * CellSize + 50;
    public const int HandStartY = 50;
    public const int HandCardSpacing = 200;

    public const int QuitButtonX = WindowWidth - 110;
    public const int QuitButtonY = 20;
    public const int QuitButtonWidth = 90;
    public const int QuitButtonHeight = 34;

    public const int RestartButtonX = QuitButtonX - 110;
    public const int RestartButtonY = QuitButtonY;
    public const int RestartButtonWidth = 100;
    public const int RestartButtonHeight = QuitButtonHeight;

    public const int DiscardButtonX = HandStartX;
    public const int DiscardButtonY = WindowHeight - 45;
    public const int DiscardButtonWidth = 130;
    public const int DiscardButtonHeight = 30;

    public const int RotateButtonWidth = 60;
    public const int RotateButtonHeight = DiscardButtonHeight;
    public const int RotateButtonY = DiscardButtonY;
    public const int RotateCCWButtonX = DiscardButtonX + DiscardButtonWidth + 10;
    public const int RotateCWButtonX = RotateCCWButtonX + RotateButtonWidth + 5;

    public static readonly Color ColorHandSizeLabel = new Color(255, 220, 60, 255);
    public const int HandSizeFontSize = 12;

    public static readonly Color BackgroundColor = new Color(20, 20, 30, 255);
    public static readonly Color GridColor = new Color(40, 40, 50, 255);

    public static readonly Color ColorBlocked = new Color(30, 30, 40, 255);
    public static readonly Color ColorPassable = new Color(80, 80, 90, 255);
    public static readonly Color ColorHole = new Color(10, 10, 15, 255);
    public static readonly Color ColorItem = new Color(70, 70, 85, 255);
    public static readonly Color ColorItemLabel = new Color(220, 220, 220, 255);
    public const int ItemLabelFontSize = 10;

    public static readonly Color ColorReachable = new Color(100, 200, 100, 150);
    public static readonly Color ColorHighlight = new Color(255, 255, 255, 100);
    public static readonly Color ColorSelectedCard = new Color(255, 220, 60, 255);
    public const int SelectedCardBorderThickness = 3;
}
