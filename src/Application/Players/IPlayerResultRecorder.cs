using Domain.Domino;

namespace Application.Players;

public interface IPlayerResultRecorder
{
    public void RecordMove(TurnState turn, TileMove move);

    public void Record(bool win);
}