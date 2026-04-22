using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using CardPathways.Data;
using CardPathways.Game;
using CardPathways.Logic;

namespace CardPathways.Rendering;

public class Renderer
{
    private readonly GameController _gameController;
    private CardDefinition? _selectedCard;

    public Renderer(GameController gameController)
    {
        _gameController = gameController;
    }

    public void Update()
    {
        HandleInput();
        Draw();
    }

    private void HandleInput()
    {
        if (_gameController.State.Status != GameStatus.Playing)
            return;

        Vector2 mousePos = Raylib.GetMousePosition();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            // Check hand clicks
            for (int i = 0; i < _gameController.State.Hand.Count; i++)
            {
                var cardRect = new Rectangle(Layout.HandStartX, Layout.HandStartY + i * Layout.HandCardSpacing, Layout.CellSize, Layout.CellSize);
                if (Raylib.CheckCollisionPointRec(mousePos, cardRect))
                {
                    _selectedCard = _gameController.State.Hand[i];
                    return;
                }
            }

            // Check map clicks
            var selected = _selectedCard;
            if (selected != null)
            {
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        var targetCoord = new MapCoord(col, row);
                        var cellRect = new Rectangle(
                            Layout.MapStartX + col * (Layout.CellSize + Layout.CellPadding),
                            Layout.MapStartY + row * (Layout.CellSize + Layout.CellPadding),
                            Layout.CellSize, Layout.CellSize);

                        if (Raylib.CheckCollisionPointRec(mousePos, cellRect))
                        {
                            if (_gameController.TryPlayCard(selected, targetCoord))
                            {
                                _selectedCard = null;
                            }
                        }
                    }
                }
            }
        }
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Layout.BackgroundColor);

        DrawMap();
        DrawHand();
        DrawUI();

        Raylib.EndDrawing();
    }

    private void DrawMap()
    {
        var state = _gameController.State;

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                var coord = new MapCoord(col, row);
                int startX = Layout.MapStartX + col * (Layout.CellSize + Layout.CellPadding);
                int startY = Layout.MapStartY + row * (Layout.CellSize + Layout.CellPadding);

                // Entry / Exit logic labeling
                if (col == 0 && row == 4) Raylib.DrawText("ENTRY", startX, startY + Layout.CellSize + 5, 10, Color.White);
                if (col == 4 && row == 0) Raylib.DrawText("EXIT", startX, startY - 15, 10, Color.White);

                if (coord == state.CurrentCell)
                {
                    DrawActiveCell(startX, startY);
                }
                else
                {
                    DrawInactiveCell(coord, startX, startY);
                }

                // Highlight valid targets
                if (_selectedCard != null && state.Status == GameStatus.Playing)
                {
                    if (GameLogic.IsValidMove(state.CurrentCell, state.Reachable, _selectedCard, coord))
                    {
                        Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.Yellow);
                        Raylib.DrawRectangle(startX, startY, Layout.CellSize, Layout.CellSize, Layout.ColorHighlight);
                    }
                }
            }
        }
    }

    private void DrawActiveCell(int startX, int startY)
    {
        var state = _gameController.State;
        var composite = GameLogic.BuildCompositeGrid(state.Map[state.CurrentCell.Row, state.CurrentCell.Col], state.ActiveCardGrid);

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var cellX = startX + c * Layout.SubCellSize;
                var cellY = startY + r * Layout.SubCellSize;

                Color color = GetSubCellColor(composite[r, c]);
                Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, color);
                Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);

                if (state.Reachable.Contains(new SubCoord(c, r)))
                {
                    Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.ColorReachable);
                }
            }
        }

        Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.White);
    }

    private void DrawInactiveCell(MapCoord coord, int startX, int startY)
    {
        var state = _gameController.State;
        var tile = state.Map[coord.Row, coord.Col];

        Raylib.DrawRectangle(startX, startY, Layout.CellSize, Layout.CellSize, Layout.ColorBlocked);

        // Draw the 3x3 tile in the center
        int offset = 3 * Layout.SubCellSize;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var cellX = startX + offset + c * Layout.SubCellSize;
                var cellY = startY + offset + r * Layout.SubCellSize;

                Color color = GetSubCellColor(tile.Grid[r, c]);
                Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, color);
                Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);
            }
        }

        Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.DarkGray);
    }

    private void DrawHand()
    {
        var state = _gameController.State;

        Raylib.DrawText("HAND", Layout.HandStartX, 20, 20, Color.White);

        for (int i = 0; i < state.Hand.Count; i++)
        {
            var card = state.Hand[i];
            int startX = Layout.HandStartX;
            int startY = Layout.HandStartY + i * Layout.HandCardSpacing;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var cellX = startX + c * Layout.SubCellSize;
                    var cellY = startY + r * Layout.SubCellSize;

                    Color color = GetSubCellColor(card.Grid[r, c]);
                    Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, color);
                    Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);
                }
            }

            if (_selectedCard == card)
            {
                Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.Yellow);
            }
            else
            {
                Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.Gray);
            }
        }
    }

    private void DrawUI()
    {
        var state = _gameController.State;

        Raylib.DrawText($"Deck: {state.Deck.Count}", Layout.HandStartX + 150, 20, 20, Color.White);
        Raylib.DrawText($"Discard: {state.Discard.Count}", Layout.HandStartX + 250, 20, 20, Color.White);

        if (state.Status == GameStatus.Won)
        {
            Raylib.DrawText("YOU WIN!", Layout.WindowWidth / 2 - 100, Layout.WindowHeight / 2 - 50, 50, Color.Green);
        }
        else if (state.Status == GameStatus.Lost)
        {
            Raylib.DrawText("GAME OVER", Layout.WindowWidth / 2 - 120, Layout.WindowHeight / 2 - 50, 50, Color.Red);
        }
    }

    private Color GetSubCellColor(SubCell cell)
    {
        return cell switch
        {
            SubCell.Blocked => Layout.ColorBlocked,
            SubCell.Passable => Layout.ColorPassable,
            SubCell.Hole => Layout.ColorHole,
            SubCell.Door => Layout.ColorDoor,
            SubCell.Key => Layout.ColorKey,
            SubCell.Shuffle => Layout.ColorShuffle,
            _ => Color.Magenta
        };
    }
}
