using System;
using System.IO;
using Raylib_cs;
using CardPathways.Data;
using CardPathways.Game;
using CardPathways.Rendering;

string basePath = AppDomain.CurrentDomain.BaseDirectory;
string cardsPath = Path.Combine(basePath, "assets", "data", "cards.json");
string tilesPath = Path.Combine(basePath, "assets", "data", "tiles.json");

var cards = DataLoader.LoadCards(cardsPath);
var tiles = DataLoader.LoadTiles(tilesPath);

var gameController = new GameController(cards, tiles);
var renderer = new Renderer(gameController);

Raylib.InitWindow(Layout.WindowWidth, Layout.WindowHeight, "Card Pathways");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    renderer.Update();
}

Raylib.CloseWindow();
