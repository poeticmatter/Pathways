namespace CardPathways.Data;

public enum SubCell
{
    Blocked,
    Passable,
    Hole,
    Door,
    Key,
    Shuffle
}

public enum Direction
{
    Top,
    Right,
    Bottom,
    Left
}

public enum GameStatus
{
    Playing,
    Won,
    Lost
}
