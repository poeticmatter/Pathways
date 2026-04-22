using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CardPathways.Data;

public static class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private class JsonCard
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public int HandSize { get; set; }
        public string[] Grid { get; set; } = [];
    }

    private class JsonTile
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string[] Grid { get; set; } = [];
    }

    public static List<CardDefinition> LoadCards(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonCards = JsonSerializer.Deserialize<List<JsonCard>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize cards from '{filePath}': result was null");

        var cards = new List<CardDefinition>();
        foreach (var jc in jsonCards)
        {
            var grid = new SubCell[9, 9];
            for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                    grid[row, col] = ParseSubCell(jc.Grid[row][col], filePath);

            cards.Add(new CardDefinition { Id = jc.Id, Role = ParseCardRole(jc.Role, filePath), HandSize = jc.HandSize, Grid = grid });
        }
        return cards;
    }

    public static List<MapTile> LoadTiles(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonTiles = JsonSerializer.Deserialize<List<JsonTile>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize tiles from '{filePath}': result was null");

        var tiles = new List<MapTile>();
        foreach (var jt in jsonTiles)
        {
            var grid = new SubCell[3, 3];
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    grid[row, col] = ParseSubCell(jt.Grid[row][col], filePath);

            tiles.Add(new MapTile { DefinitionId = jt.Id, Role = ParseTileRole(jt.Role, filePath), Grid = grid });
        }
        return tiles;
    }

    private static CardRole ParseCardRole(string? role, string filePath) => role switch
    {
        null or "normal" => CardRole.Normal,
        "start"          => CardRole.Start,
        _                => throw new InvalidOperationException($"Unknown card role '{role}' in '{filePath}'")
    };

    private static TileRole ParseTileRole(string? role, string filePath) => role switch
    {
        null or "normal" => TileRole.Normal,
        "start"          => TileRole.Start,
        _                => throw new InvalidOperationException($"Unknown tile role '{role}' in '{filePath}'")
    };

    private static SubCell ParseSubCell(char c, string filePath) => c switch
    {
        ' ' => SubCell.Blocked,
        '#' => SubCell.Passable,
        'H' => SubCell.Hole,
        'D' => SubCell.Door,
        'K' => SubCell.Key,
        'S' => SubCell.Shuffle,
        _ => throw new InvalidOperationException($"Unknown subcell character '{c}' in '{filePath}'")
    };
}
