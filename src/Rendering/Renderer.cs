using System.Collections.Generic;
using Raylib_cs;
using CardPathways.Data;
using CardPathways.Game;
using CardPathways.Logic;

namespace CardPathways.Rendering;

public class Renderer
{
    private readonly GameController _gameController;
    private readonly IReadOnlyGameState _state;

    private CardDefinition? _selectedCard;
    private HashSet<MapCoord>? _validTargets;

    private string? _statusMessage;
    private double _statusMessageExpiry;

    public Renderer(GameController gameController)
    {
        _gameController = gameController;
        _state = gameController.State;
    }

    public void Update()
    {
        HandleInput();
        Draw();
    }

    private void HandleInput()
    {
        if (_state.Status != GameStatus.Playing)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R))
                Restart();
            return;
        }

        var mousePos = Raylib.GetMousePosition();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            for (int i = 0; i < _state.Hand.Count; i++)
            {
                var cardRect = new Rectangle(
                    Layout.HandStartX,
                    Layout.HandStartY + i * Layout.HandCardSpacing,
                    Layout.CellSize,
                    Layout.CellSize);

                if (Raylib.CheckCollisionPointRec(mousePos, cardRect))
                {
                    SelectCard(_state.Hand[i]);
                    return;
                }
            }

            if (_selectedCard != null)
            {
                for (int row = 0; row < _state.Map.GetLength(0); row++)
                {
                    for (int col = 0; col < _state.Map.GetLength(1); col++)
                    {
                        var cellRect = new Rectangle(
                            Layout.MapStartX + col * (Layout.CellSize + Layout.CellPadding),
                            Layout.MapStartY + row * (Layout.CellSize + Layout.CellPadding),
                            Layout.CellSize,
                            Layout.CellSize);

                        if (Raylib.CheckCollisionPointRec(mousePos, cellRect))
                        {
                            var targetCoord = new MapCoord(col, row);
                            if (_gameController.TryPlayCard(_selectedCard, targetCoord))
                            {
                                _selectedCard = null;
                                _validTargets = null;
                            }
                            else
                            {
                                ShowMessage("Invalid move");
                            }
                            return;
                        }
                    }
                }
            }
        }
    }

    private void SelectCard(CardDefinition card)
    {
        _selectedCard = card;
        _validTargets = ComputeValidTargets(card);
    }

    private HashSet<MapCoord> ComputeValidTargets(CardDefinition card)
    {
        var targets = new HashSet<MapCoord>();
        int mapRows = _state.Map.GetLength(0);
        int mapCols = _state.Map.GetLength(1);
        for (int row = 0; row < mapRows; row++)
        {
            for (int col = 0; col < mapCols; col++)
            {
                var coord = new MapCoord(col, row);
                if (GameLogic.IsValidMove(_state.CurrentCell, _state.Reachable, card, coord))
                    targets.Add(coord);
            }
        }
        return targets;
    }

    private void Restart()
    {
        _gameController.Reset();
        _selectedCard = null;
        _validTargets = null;
    }

    private void ShowMessage(string message)
    {
        _statusMessage = message;
        _statusMessageExpiry = Raylib.GetTime() + 2.0;
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
        for (int row = 0; row < _state.Map.GetLength(0); row++)
        {
            for (int col = 0; col < _state.Map.GetLength(1); col++)
            {
                var coord = new MapCoord(col, row);
                int startX = Layout.MapStartX + col * (Layout.CellSize + Layout.CellPadding);
                int startY = Layout.MapStartY + row * (Layout.CellSize + Layout.CellPadding);

                if (col == 0 && row == 4) Raylib.DrawText("ENTRY", startX, startY + Layout.CellSize + 5, 10, Color.White);
                if (col == 4 && row == 0) Raylib.DrawText("EXIT", startX, startY - 15, 10, Color.White);

                if (coord == _state.CurrentCell)
                    DrawActiveCell(startX, startY);
                else
                    DrawInactiveCell(coord, startX, startY);

                if (_validTargets != null && _state.Status == GameStatus.Playing && _validTargets.Contains(coord))
                {
                    Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.Yellow);
                    Raylib.DrawRectangle(startX, startY, Layout.CellSize, Layout.CellSize, Layout.ColorHighlight);
                }
            }
        }
    }

    private void DrawActiveCell(int startX, int startY)
    {
        var composite = _state.ActiveCompositeGrid;

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int cellX = startX + c * Layout.SubCellSize;
                int cellY = startY + r * Layout.SubCellSize;

                Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, GetSubCellColor(composite[r, c]));
                Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);

                if (_state.Reachable.Contains(new SubCoord(c, r)))
                    Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.ColorReachable);
            }
        }

        Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.White);
    }

    private void DrawInactiveCell(MapCoord coord, int startX, int startY)
    {
        var tile = _state.Map[coord.Row, coord.Col];
        Raylib.DrawRectangle(startX, startY, Layout.CellSize, Layout.CellSize, Layout.ColorBlocked);

        int offset = 3 * Layout.SubCellSize;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int cellX = startX + offset + c * Layout.SubCellSize;
                int cellY = startY + offset + r * Layout.SubCellSize;

                Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, GetSubCellColor(tile.Grid[r, c]));
                Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);
            }
        }

        Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, Color.DarkGray);
    }

    private void DrawHand()
    {
        Raylib.DrawText("HAND", Layout.HandStartX, 20, 20, Color.White);

        for (int i = 0; i < _state.Hand.Count; i++)
        {
            var card = _state.Hand[i];
            int startX = Layout.HandStartX;
            int startY = Layout.HandStartY + i * Layout.HandCardSpacing;

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    int cellX = startX + c * Layout.SubCellSize;
                    int cellY = startY + r * Layout.SubCellSize;

                    Raylib.DrawRectangle(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, GetSubCellColor(card.Grid[r, c]));
                    Raylib.DrawRectangleLines(cellX, cellY, Layout.SubCellSize, Layout.SubCellSize, Layout.GridColor);
                }
            }

            var borderColor = (_selectedCard == card) ? Color.Yellow : Color.Gray;
            Raylib.DrawRectangleLines(startX, startY, Layout.CellSize, Layout.CellSize, borderColor);
        }
    }

    private void DrawUI()
    {
        Raylib.DrawText($"Deck: {_state.DeckCount}", Layout.HandStartX + 150, 20, 20, Color.White);
        Raylib.DrawText($"Discard: {_state.DiscardCount}", Layout.HandStartX + 250, 20, 20, Color.White);

        if (_statusMessage != null && Raylib.GetTime() < _statusMessageExpiry)
            Raylib.DrawText(_statusMessage, Layout.MapStartX, Layout.WindowHeight - 30, 18, Color.Orange);

        if (_state.Status == GameStatus.Won)
        {
            Raylib.DrawText("YOU WIN!", Layout.WindowWidth / 2 - 100, Layout.WindowHeight / 2 - 50, 50, Color.Green);
            Raylib.DrawText("Press R to play again", Layout.WindowWidth / 2 - 110, Layout.WindowHeight / 2 + 20, 20, Color.White);
        }
        else if (_state.Status == GameStatus.Lost)
        {
            Raylib.DrawText("GAME OVER", Layout.WindowWidth / 2 - 120, Layout.WindowHeight / 2 - 50, 50, Color.Red);
            Raylib.DrawText("Press R to play again", Layout.WindowWidth / 2 - 110, Layout.WindowHeight / 2 + 20, 20, Color.White);
        }
    }

    private static Color GetSubCellColor(SubCell cell) => cell switch
    {
        SubCell.Blocked  => Layout.ColorBlocked,
        SubCell.Passable => Layout.ColorPassable,
        SubCell.Hole     => Layout.ColorHole,
        SubCell.Door     => Layout.ColorDoor,
        SubCell.Key      => Layout.ColorKey,
        SubCell.Shuffle  => Layout.ColorShuffle,
        _                => Color.Magenta
    };
}
