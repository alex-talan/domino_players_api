namespace Domain.Domino;

internal static class TileSelectionRules
{
    public static bool IsPlayable(DominoTile tile, TurnState turn) => turn.Table.Count == 0
        || tile.Matches(turn.Head!.Value)
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