using Domain.Domino;

namespace Application.Players;

public interface IPlayerResultRecorder
{
    public void RecordMove(TurnState turn, TileMove move, int? turnNumber = null);

    public void Record(bool win, IReadOnlyList<int>? finalTiles = null);
}