using System;

namespace CardPathways.Data;

public class CardDefinition
{
    public int Id { get; init; }
    public CardRole Role { get; init; }
    public int HandSize { get; init; }
    public SubCell[,] Grid { get; init; } = new SubCell[9, 9];

    public CardDefinition Clone()
    {
        var clonedGrid = new SubCell[9, 9];
        Array.Copy(Grid, clonedGrid, Grid.Length);
        return new CardDefinition
        {
            Id = Id,
            Role = Role,
            HandSize = HandSize,
            Grid = clonedGrid
        };
    }
}

public class MapTile
{
    public int DefinitionId { get; init; }
    public TileRole Role { get; init; }
    public SubCell[,] Grid { get; init; } = new SubCell[3, 3];

    public MapTile Clone()
    {
        var clonedGrid = new SubCell[3, 3];
        Array.Copy(Grid, clonedGrid, Grid.Length);
        return new MapTile
        {
            DefinitionId = DefinitionId,
            Role = Role,
            Grid = clonedGrid
        };
    }
}
