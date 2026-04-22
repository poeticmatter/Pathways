using System.Collections.Generic;

namespace CardPathways.Data;

public readonly record struct MapCoord(int Col, int Row);
public readonly record struct SubCoord(int Col, int Row);

public record PlacementResult(
    MapTile ModifiedTile,
    SubCell[,] ModifiedCardGrid,
    HashSet<SubCoord> Reachable,
    bool TriggeredReshuffle
);
