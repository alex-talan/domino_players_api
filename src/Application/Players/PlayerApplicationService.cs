using Domain.Domino;

namespace Application.Players;

public sealed class PlayerApplicationService(
    ITileSelectionStrategy tileSelectionStrategy,
    IPlayerResultRecorder playerResultRecorder)
{
    public TileMove SelectMove(TurnState turn)
    {
        TileMove move = tileSelectionStrategy.SelectMove(turn);
        playerResultRecorder.RecordMove(turn, move);
        return move;
    }

    public void RecordResult(bool win, IReadOnlyList<int>? finalTiles = null) => playerResultRecorder.Record(win, finalTiles);
}