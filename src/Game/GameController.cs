using System;
using System.Collections.Generic;
using System.Linq;
using CardPathways.Data;
using CardPathways.Logic;

namespace CardPathways.Game;

public class GameController
{
    private readonly GameState _state;
    private readonly List<CardDefinition> _allCards;
    private readonly List<MapTile> _allTiles;

    public IReadOnlyGameState State => _state;

    public GameController(List<CardDefinition> allCards, List<MapTile> allTiles)
    {
        _allCards = allCards;
        _allTiles = allTiles;
        _state = new GameState();
        Initialize();
    }

    public void Reset() => Initialize();

    private void Initialize()
    {
        _state.Deck.Clear();
        _state.Hand.Clear();
        _state.Discard.Clear();
        _state.Status = GameStatus.Playing;

        var random = new Random();

        var startCard = _allCards.FirstOrDefault(c => c.Role == CardRole.Start)
            ?? throw new InvalidOperationException("No card with role 'start' found in cards.json");
        var deckCards = _allCards.Where(c => c.Role != CardRole.Start).ToList();
        _state.Deck.AddRange(deckCards.OrderBy(x => random.Next()));

        var reshuffleTile = _allTiles.FirstOrDefault(t => t.Grid[1, 1] == SubCell.Shuffle)
            ?? throw new InvalidOperationException("No tile with a Shuffle sub-cell at center (1,1) found in tiles.json");
        var startTile = _allTiles.FirstOrDefault(t => t.Role == TileRole.Start)
            ?? throw new InvalidOperationException("No tile with role 'start' found in tiles.json");
        var poolTiles = _allTiles.Where(t => t.DefinitionId != reshuffleTile.DefinitionId && t.Role != TileRole.Start).ToList();

        int mapRows = _state.Map.GetLength(0);
        int mapCols = _state.Map.GetLength(1);
        int poolCount = mapRows * mapCols - 2;

        if (poolTiles.Count < poolCount)
            throw new InvalidOperationException(
                $"tiles.json has only {poolTiles.Count} pool tiles; {poolCount} are required to fill the map.");

        // Entry cell (0,4): player enters from the left edge
        var startCell = new MapCoord(Col: 0, Row: 4);
        const Direction startEntryEdge = Direction.Left;

        const int reshuffleRow = 2;
        const int reshuffleCol = 2;

        var shuffledTiles = poolTiles.OrderBy(x => random.Next()).Take(poolCount).ToList();
        int tileIndex = 0;
        for (int row = 0; row < mapRows; row++)
        {
            for (int col = 0; col < mapCols; col++)
            {
                _state.Map[row, col] = (col == reshuffleCol && row == reshuffleRow) ? reshuffleTile.Clone()
                    : (col == startCell.Col && row == startCell.Row)               ? startTile.Clone()
                    :                                                                 shuffledTiles[tileIndex++].Clone();
            }
        }

        _state.CurrentCell = startCell;
        _state.EntryEdge = startEntryEdge;

        var placement = GameLogic.ResolvePlacement(_state.Map[startCell.Row, startCell.Col], startCard, startEntryEdge);
        _state.Map[startCell.Row, startCell.Col] = placement.ModifiedTile;
        _state.ActiveCardGrid = placement.ModifiedCardGrid;
        _state.Reachable = placement.Reachable;
        _state.ActiveCardDef = startCard;
        _state.ActiveCompositeGrid = GameLogic.BuildCompositeGrid(
            _state.Map[startCell.Row, startCell.Col], _state.ActiveCardGrid);

        NormalizeHand();
    }

    private void NormalizeHand()
    {
        int targetSize = _state.ActiveCardDef.HandSize;

        while (_state.Hand.Count > targetSize)
        {
            _state.Discard.Add(_state.Hand[^1]);
            _state.Hand.RemoveAt(_state.Hand.Count - 1);
        }

        while (_state.Hand.Count < targetSize && _state.Deck.Count > 0)
        {
            _state.Hand.Add(_state.Deck[0]);
            _state.Deck.RemoveAt(0);
        }
    }

    public bool TryDiscardCard(CardDefinition card)
    {
        if (_state.Status != GameStatus.Playing) return false;
        if (!_state.Hand.Contains(card)) return false;

        _state.Hand.Remove(card);
        _state.Discard.Add(card);
        NormalizeHand();

        if (_state.Hand.Count == 0 && _state.Deck.Count == 0)
            _state.Status = GameStatus.Lost;

        return true;
    }

    public bool TryPlayCard(CardDefinition card, MapCoord targetCell)
    {
        if (_state.Status != GameStatus.Playing) return false;
        if (!_state.Hand.Contains(card)) return false;
        if (!GameLogic.IsValidMove(_state.CurrentCell, _state.Reachable, card, targetCell)) return false;

        var entryDirection = GameLogic.GetEntryDirectionFromMove(_state.CurrentCell, targetCell);
        var result = GameLogic.ResolvePlacement(_state.Map[targetCell.Row, targetCell.Col], card, entryDirection);

        _state.Map[targetCell.Row, targetCell.Col] = result.ModifiedTile;
        _state.ActiveCardGrid = result.ModifiedCardGrid;
        _state.Reachable = result.Reachable;
        _state.CurrentCell = targetCell;
        _state.EntryEdge = entryDirection;
        _state.ActiveCompositeGrid = GameLogic.BuildCompositeGrid(result.ModifiedTile, result.ModifiedCardGrid);

        var previousActiveDef = _state.ActiveCardDef;
        _state.ActiveCardDef = card;

        if (previousActiveDef.Role != CardRole.Start)
            _state.Discard.Add(previousActiveDef);

        if (result.TriggeredReshuffle)
        {
            _state.Deck.AddRange(_state.Discard);
            _state.Discard.Clear();
            var shuffledDeck = _state.Deck.OrderBy(x => Random.Shared.Next()).ToList();
            _state.Deck.Clear();
            _state.Deck.AddRange(shuffledDeck);
        }

        _state.Hand.Remove(card);
        NormalizeHand();

        if (_state.CurrentCell.Col == 4 && _state.CurrentCell.Row == 0)
        {
            var topExit = GameLogic.GetExitPoint(Direction.Top);
            var rightExit = GameLogic.GetExitPoint(Direction.Right);
            if (_state.Reachable.Contains(topExit) || _state.Reachable.Contains(rightExit))
                _state.Status = GameStatus.Won;
        }

        if (_state.Status == GameStatus.Playing && _state.Hand.Count == 0 && _state.Deck.Count == 0)
            _state.Status = GameStatus.Lost;

        return true;
    }
}
