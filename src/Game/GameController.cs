using System;
using System.Collections.Generic;
using System.Linq;
using CardPathways.Data;
using CardPathways.Logic;

namespace CardPathways.Game;

public class GameController
{
    public GameState State { get; }

    public GameController(List<CardDefinition> allCards, List<MapTile> allTiles)
    {
        State = new GameState();
        Initialize(allCards, allTiles);
    }

    private void Initialize(List<CardDefinition> allCards, List<MapTile> allTiles)
    {
        var random = new Random();

        // Separate cards
        var startCard = allCards.First(c => c.Id == 0);
        var deckCards = allCards.Where(c => c.Id != 0).ToList();

        // Shuffle deck
        State.Deck.AddRange(deckCards.OrderBy(x => random.Next()));

        // Separate tiles
        var reshuffleTile = allTiles.FirstOrDefault(t => t.Grid[1, 1] == SubCell.Shuffle)
            ?? throw new InvalidOperationException("No tile with a Shuffle sub-cell at center (1,1) found in tiles.json");
        var standardTiles = allTiles.Where(t => t.DefinitionId != reshuffleTile.DefinitionId).ToList();

        // There might be more standard tiles than needed, shuffle and take 24
        var shuffledStandardTiles = standardTiles.OrderBy(x => random.Next()).Take(24).ToList();

        int tileIndex = 0;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (col == 2 && row == 2)
                {
                    State.Map[row, col] = reshuffleTile.Clone();
                }
                else
                {
                    State.Map[row, col] = shuffledStandardTiles[tileIndex++].Clone();
                }
            }
        }

        // Setup start
        State.CurrentCell = new MapCoord(0, 4);
        State.EntryEdge = Direction.Left; // Player enters from the left edge

        // Compute initial placement for start card
        var initialTile = State.Map[4, 0];
        var placement = GameLogic.ResolvePlacement(initialTile, startCard, State.EntryEdge);

        State.Map[4, 0] = placement.ModifiedTile;
        State.ActiveCardGrid = placement.ModifiedCardGrid;
        State.Reachable = placement.Reachable;
        State.ActiveCardDef = startCard;

        NormalizeHand();
    }

    public void NormalizeHand()
    {
        int targetSize = State.ActiveCardDef.HandSize;

        // Discard excess
        while (State.Hand.Count > targetSize)
        {
            State.Discard.Add(State.Hand[^1]);
            State.Hand.RemoveAt(State.Hand.Count - 1);
        }

        // Draw cards
        while (State.Hand.Count < targetSize && State.Deck.Count > 0)
        {
            State.Hand.Add(State.Deck[0]);
            State.Deck.RemoveAt(0);
        }
    }

    public bool TryPlayCard(CardDefinition card, MapCoord targetCell)
    {
        if (State.Status != GameStatus.Playing) return false;

        if (!State.Hand.Contains(card)) return false;

        if (!GameLogic.IsValidMove(State.CurrentCell, State.Reachable, card, targetCell))
        {
            return false;
        }

        // 1. Play card (valid move passed)
        var entryDirection = GameLogic.GetEntryDirectionFromMove(State.CurrentCell, targetCell);
        var targetTile = State.Map[targetCell.Row, targetCell.Col];

        // 2. Resolve placement
        var result = GameLogic.ResolvePlacement(targetTile, card, entryDirection);

        // 3. Update map
        State.Map[targetCell.Row, targetCell.Col] = result.ModifiedTile;
        State.ActiveCardGrid = result.ModifiedCardGrid;
        State.Reachable = result.Reachable;
        State.CurrentCell = targetCell;
        State.EntryEdge = entryDirection;

        var previousActiveDef = State.ActiveCardDef;
        State.ActiveCardDef = card;

        // 4. Discard old card
        if (previousActiveDef.Id != 0)
        {
            State.Discard.Add(previousActiveDef);
        }

        // 5. Apply reshuffle
        if (result.TriggeredReshuffle)
        {
            State.Deck.AddRange(State.Discard);
            State.Discard.Clear();
            var shuffledDeck = State.Deck.OrderBy(x => Random.Shared.Next()).ToList();
            State.Deck.Clear();
            State.Deck.AddRange(shuffledDeck);
        }

        // 6. Normalize hand
        State.Hand.Remove(card);
        NormalizeHand();

        // 7. Check win
        if (State.CurrentCell.Col == 4 && State.CurrentCell.Row == 0)
        {
            var topExit = GameLogic.GetExitPoint(Direction.Top);
            var rightExit = GameLogic.GetExitPoint(Direction.Right);
            if (State.Reachable.Contains(topExit) || State.Reachable.Contains(rightExit))
            {
                State.Status = GameStatus.Won;
            }
        }

        // 8. Check loss
        if (State.Status == GameStatus.Playing && State.Hand.Count == 0 && State.Deck.Count == 0)
        {
            State.Status = GameStatus.Lost;
        }

        return true;
    }
}
