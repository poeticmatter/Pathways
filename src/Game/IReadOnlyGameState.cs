using System.Collections.Generic;
using CardPathways.Data;

namespace CardPathways.Game;

public interface IReadOnlyGameState
{
    MapTile[,] Map { get; }
    IReadOnlyList<CardDefinition> Hand { get; }
    int DeckCount { get; }
    int DiscardCount { get; }
    MapCoord CurrentCell { get; }
    SubCell[,] ActiveCompositeGrid { get; }
    IReadOnlySet<SubCoord> Reachable { get; }
    GameStatus Status { get; }
}
