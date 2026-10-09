using Application.Players;
using Domain.Domino;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Players;

public sealed class LoggingPlayerResultRecorder(
    ILogger<LoggingPlayerResultRecorder> logger,
    IOptions<PlayerOptions> playerOptions) : IPlayerResultRecorder
{
    public void RecordMove(TurnState turn, TileMove move)
    {
        logger.LogInformation(
            "[{PlayerName}] TABLE:[{Table}] - My Tiles:[{YourTiles}] - My Move:({Tile}, {Position})",
            playerOptions.Value.Name,
            string.Join(", ", turn.Table),
            string.Join(", ", turn.YourTiles),
            move.Tile,
            move.Position);
    }

    public void Record(bool win)
    {
        if (win)
        {
            logger.LogInformation("[{PlayerName}] I win!!", playerOptions.Value.Name);
        }
        else
        {
            logger.LogInformation("[{PlayerName}] I did not win.", playerOptions.Value.Name);
        }
    }
}