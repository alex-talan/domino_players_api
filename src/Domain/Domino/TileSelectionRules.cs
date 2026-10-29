namespace Domain.Domino;

internal static class TileSelectionRules
{
    public static TileMove? GetOpeningMove(TurnState turn)
    {
        if (turn.Table.Count != 0)
        {
            return null;
        }

        if (!turn.YourTiles.Contains(TileCatalogue.DoubleSixId))
        {
            throw new ArgumentException("The opening hand must contain the double-six tile.", nameof(turn));
        }

        return new TileMove(TileCatalogue.DoubleSixId, "head");
    }

    public static bool IsPlayable(DominoTile tile, TurnState turn) => turn.Table.Count == 0
        ? tile.Id == TileCatalogue.DoubleSixId
        : tile.Matches(turn.Head!.Value)
        || tile.Matches(turn.Tail!.Value);

    public static string GetDefaultPosition(DominoTile tile, TurnState turn)
    {
        if (turn.Table.Count == 0)
        {
            return "head";
        }

        bool matchesHead = tile.Matches(turn.Head!.Value);
        bool matchesTail = tile.Matches(turn.Tail!.Value);

        return matchesHead ? "head" : matchesTail ? "tail" : string.Empty;
    }
}