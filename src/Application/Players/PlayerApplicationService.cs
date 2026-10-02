using Domain.Domino;

namespace Application.Players;

public sealed class PlayerApplicationService(
    ITileSelectionStrategy tileSelectionStrategy,
    IPlayerResultRecorder playerResultRecorder)
{
    public TileMove SelectMove(TurnState turn) => tileSelectionStrategy.SelectMove(turn);

    public void RecordResult(bool win) => playerResultRecorder.Record(win);
}