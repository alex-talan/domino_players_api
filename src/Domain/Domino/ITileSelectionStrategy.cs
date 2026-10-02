namespace Domain.Domino;

public interface ITileSelectionStrategy
{
    public TileMove SelectMove(TurnState turn);
}