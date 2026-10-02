namespace Domain.Domino;

public sealed class GreedyTileSelectionStrategy : ITileSelectionStrategy
{
    public TileMove SelectMove(TurnState turn)
    {
        List<DominoTile> playableTiles = turn.YourTiles
            .Select(TileCatalogue.GetById)
            .Where(tile => TileSelectionRules.IsPlayable(tile, turn))
            .ToList();

        if (playableTiles.Count == 0)
        {
            return TileMove.Pass;
        }

        int highestScore = playableTiles.Max(tile => tile.Points);
        List<DominoTile> highestScoringTiles = playableTiles
            .Where(tile => tile.Points == highestScore)
            .ToList();
        DominoTile selectedTile = highestScoringTiles[Random.Shared.Next(highestScoringTiles.Count)];

        return new TileMove(selectedTile.Id, SelectPosition(selectedTile, turn));
    }

    private static string SelectPosition(DominoTile tile, TurnState turn)
    {
        string defaultPosition = TileSelectionRules.GetDefaultPosition(tile, turn);
        if (defaultPosition is not ("head" or "tail") || turn.Table.Count == 0)
        {
            return defaultPosition;
        }

        bool matchesHead = tile.Matches(turn.Head!.Value);
        bool matchesTail = tile.Matches(turn.Tail!.Value);
        if (!matchesHead || !matchesTail)
        {
            return defaultPosition;
        }

        int remainingAtHead = CountCompatibleTiles(tile, tile.OtherEnd(turn.Head.Value), turn.Tail.Value, turn);
        int remainingAtTail = CountCompatibleTiles(tile, turn.Head.Value, tile.OtherEnd(turn.Tail.Value), turn);

        return remainingAtHead >= remainingAtTail ? "head" : "tail";
    }

    private static int CountCompatibleTiles(DominoTile selectedTile, int newHead, int newTail, TurnState turn) =>
        turn.YourTiles
            .Where(tileId => tileId != selectedTile.Id)
            .Select(TileCatalogue.GetById)
            .Count(tile => tile.Matches(newHead) || tile.Matches(newTail));
}