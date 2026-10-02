namespace Domain.Domino;

public sealed class RandomTileSelectionStrategy : ITileSelectionStrategy
{
    public TileMove SelectMove(TurnState turn)
    {
        foreach (int tileId in turn.YourTiles)
        {
            DominoTile tile = TileCatalogue.GetById(tileId);
            if (TileSelectionRules.IsPlayable(tile, turn))
            {
                return new TileMove(tile.Id, TileSelectionRules.GetDefaultPosition(tile, turn));
            }
        }

        return TileMove.Pass;
    }
}