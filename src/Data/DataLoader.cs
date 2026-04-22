using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CardPathways.Data;

public static class DataLoader
{
    private class JsonCard
    {
        public int id { get; set; }
        public int handSize { get; set; }
        public string[][] grid { get; set; } = [];
    }

    private class JsonTile
    {
        public int id { get; set; }
        public string[][] grid { get; set; } = [];
    }

    public static List<CardDefinition> LoadCards(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonCards = JsonSerializer.Deserialize<List<JsonCard>>(json) ?? throw new InvalidOperationException($"Failed to deserialize cards from '{filePath}': result was null");

        var cards = new List<CardDefinition>();
        foreach (var jc in jsonCards)
        {
            var grid = new SubCell[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    grid[row, col] = ParseSubCell(jc.grid[row][col]);
                }
            }

            cards.Add(new CardDefinition
            {
                Id = jc.id,
                HandSize = jc.handSize,
                Grid = grid
            });
        }
        return cards;
    }

    public static List<MapTile> LoadTiles(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonTiles = JsonSerializer.Deserialize<List<JsonTile>>(json) ?? throw new InvalidOperationException($"Failed to deserialize tiles from '{filePath}': result was null");

        var tiles = new List<MapTile>();
        foreach (var jt in jsonTiles)
        {
            var grid = new SubCell[3, 3];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    grid[row, col] = ParseSubCell(jt.grid[row][col]);
                }
            }

            tiles.Add(new MapTile
            {
                DefinitionId = jt.id,
                Grid = grid
            });
        }
        return tiles;
    }

    private static SubCell ParseSubCell(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "blocked" => SubCell.Blocked,
            "passable" => SubCell.Passable,
            "hole" => SubCell.Hole,
            "door" => SubCell.Door,
            "key" => SubCell.Key,
            "shuffle" => SubCell.Shuffle,
            _ => throw new InvalidOperationException($"Unknown subcell type: '{value}'")
        };
    }
}
