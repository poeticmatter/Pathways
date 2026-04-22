using System;
using System.Collections.Generic;
using CardPathways.Data;

namespace CardPathways.Logic;

public static class GameLogic
{
    public static SubCell[,] BuildCompositeGrid(MapTile tile, SubCell[,] cardGrid)
    {
        var composite = new SubCell[9, 9];
        Array.Copy(cardGrid, composite, cardGrid.Length);

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                composite[row + 3, col + 3] = tile.Grid[row, col];
            }
        }

        return composite;
    }

    public static SubCoord GetEntryPoint(Direction entryEdge)
    {
        return entryEdge switch
        {
            Direction.Left => new SubCoord(0, 4),
            Direction.Right => new SubCoord(8, 4),
            Direction.Top => new SubCoord(4, 0),
            Direction.Bottom => new SubCoord(4, 8),
            _ => throw new ArgumentException("Invalid direction")
        };
    }

    public static SubCoord GetExitPoint(Direction exitEdge) => GetEntryPoint(exitEdge);

    // Returns reachable cells, keys encountered, doors encountered, shuffles encountered
    public static (HashSet<SubCoord> Reachable, List<SubCoord> Keys, List<SubCoord> Doors, List<SubCoord> Shuffles) ComputeReachability(SubCell[,] composite, Direction entryEdge)
    {
        var reachable = new HashSet<SubCoord>();
        var keys = new List<SubCoord>();
        var doors = new List<SubCoord>();
        var shuffles = new List<SubCoord>();

        var start = GetEntryPoint(entryEdge);
        var startCell = composite[start.Row, start.Col];

        if (startCell == SubCell.Blocked || startCell == SubCell.Hole || startCell == SubCell.Door)
        {
            return (reachable, keys, doors, shuffles);
        }

        var queue = new Queue<SubCoord>();
        queue.Enqueue(start);
        reachable.Add(start);

        if (startCell == SubCell.Key) keys.Add(start);
        if (startCell == SubCell.Shuffle) shuffles.Add(start);

        int[] dRow = { -1, 1, 0, 0 };
        int[] dCol = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nRow = curr.Row + dRow[i];
                int nCol = curr.Col + dCol[i];
                var next = new SubCoord(nCol, nRow);

                if (nRow >= 0 && nRow < 9 && nCol >= 0 && nCol < 9 && !reachable.Contains(next))
                {
                    var cell = composite[nRow, nCol];

                    if (cell == SubCell.Passable || cell == SubCell.Key || cell == SubCell.Shuffle)
                    {
                        reachable.Add(next);
                        queue.Enqueue(next);

                        if (cell == SubCell.Key) keys.Add(next);
                        if (cell == SubCell.Shuffle) shuffles.Add(next);
                    }
                    else if (cell == SubCell.Door)
                    {
                        reachable.Add(next); // Add to reachable to avoid revisiting, but don't queue to traverse past it
                        doors.Add(next);
                    }
                }
            }
        }

        return (reachable, keys, doors, shuffles);
    }

    private static bool IsInTile(SubCoord coord)
    {
        return coord.Row >= 3 && coord.Row <= 5 && coord.Col >= 3 && coord.Col <= 5;
    }

    public static PlacementResult ResolvePlacement(MapTile tile, CardDefinition card, Direction entryEdge)
    {
        var modifiedTile = tile.Clone();
        var modifiedCardGrid = new SubCell[9, 9];
        Array.Copy(card.Grid, modifiedCardGrid, card.Grid.Length);

        bool triggeredReshuffle = false;
        HashSet<SubCoord> lastReachable = new HashSet<SubCoord>();

        bool changed;
        do
        {
            changed = false;
            var composite = BuildCompositeGrid(modifiedTile, modifiedCardGrid);
            var (reachable, keys, doors, shuffles) = ComputeReachability(composite, entryEdge);
            lastReachable = reachable;

            if (keys.Count > 0 && doors.Count > 0)
            {
                int pairsToConsume = Math.Min(keys.Count, doors.Count);
                for (int i = 0; i < pairsToConsume; i++)
                {
                    var keyCoord = keys[i];
                    var doorCoord = doors[i];

                    // Set key to Passable
                    if (IsInTile(keyCoord)) modifiedTile.Grid[keyCoord.Row - 3, keyCoord.Col - 3] = SubCell.Passable;
                    else modifiedCardGrid[keyCoord.Row, keyCoord.Col] = SubCell.Passable;

                    // Set door to Passable
                    if (IsInTile(doorCoord)) modifiedTile.Grid[doorCoord.Row - 3, doorCoord.Col - 3] = SubCell.Passable;
                    else modifiedCardGrid[doorCoord.Row, doorCoord.Col] = SubCell.Passable;
                }
                changed = true;
            }

            foreach (var shuffleCoord in shuffles)
            {
                // Set shuffle to Passable
                if (IsInTile(shuffleCoord))
                {
                    if (modifiedTile.Grid[shuffleCoord.Row - 3, shuffleCoord.Col - 3] == SubCell.Shuffle)
                    {
                        modifiedTile.Grid[shuffleCoord.Row - 3, shuffleCoord.Col - 3] = SubCell.Passable;
                        triggeredReshuffle = true;
                        changed = true;
                    }
                }
                else
                {
                    if (modifiedCardGrid[shuffleCoord.Row, shuffleCoord.Col] == SubCell.Shuffle)
                    {
                        modifiedCardGrid[shuffleCoord.Row, shuffleCoord.Col] = SubCell.Passable;
                        triggeredReshuffle = true;
                        changed = true;
                    }
                }
            }
        } while (changed);

        return new PlacementResult(modifiedTile, modifiedCardGrid, lastReachable, triggeredReshuffle);
    }

    public static Direction GetEntryDirectionFromMove(MapCoord current, MapCoord target)
    {
        if (target.Col == current.Col + 1 && target.Row == current.Row) return Direction.Left;
        if (target.Col == current.Col - 1 && target.Row == current.Row) return Direction.Right;
        if (target.Col == current.Col && target.Row == current.Row + 1) return Direction.Top;
        if (target.Col == current.Col && target.Row == current.Row - 1) return Direction.Bottom;

        throw new ArgumentException("Cells are not orthogonal neighbors");
    }

    public static Direction GetExitDirectionToTarget(MapCoord current, MapCoord target)
    {
        if (target.Col == current.Col + 1 && target.Row == current.Row) return Direction.Right;
        if (target.Col == current.Col - 1 && target.Row == current.Row) return Direction.Left;
        if (target.Col == current.Col && target.Row == current.Row + 1) return Direction.Bottom;
        if (target.Col == current.Col && target.Row == current.Row - 1) return Direction.Top;

        throw new ArgumentException("Cells are not orthogonal neighbors");
    }

    public static bool IsValidMove(MapCoord currentCell, IReadOnlySet<SubCoord> currentReachable, CardDefinition candidateCard, MapCoord targetCell)
    {
        // 1. Orthogonally adjacent
        if (Math.Abs(currentCell.Col - targetCell.Col) + Math.Abs(currentCell.Row - targetCell.Row) != 1) return false;

        // 2. Map bounds
        if (targetCell.Col < 0 || targetCell.Col > 4 || targetCell.Row < 0 || targetCell.Row > 4) return false;

        // 3. Current card's reachable set includes exit facing target
        var exitDirection = GetExitDirectionToTarget(currentCell, targetCell);
        var exitCoord = GetExitPoint(exitDirection);
        if (!currentReachable.Contains(exitCoord)) return false;

        // 4. Candidate card has Passable at entry edge.
        // Entry points are always at row/col 0 or 8, which is card space — tile space starts at 3.
        var entryDirection = GetEntryDirectionFromMove(currentCell, targetCell);
        var entryCoord = GetEntryPoint(entryDirection);
        var entryCell = candidateCard.Grid[entryCoord.Row, entryCoord.Col];

        if (entryCell != SubCell.Passable && entryCell != SubCell.Key && entryCell != SubCell.Shuffle) return false;

        return true;
    }
}
