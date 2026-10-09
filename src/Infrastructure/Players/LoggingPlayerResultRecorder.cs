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
    public void RecordMove(TurnState turn, TileMove move, int? turnNumber = null)
    {
        logger.LogInformation(
            "[{PlayerName}] TURN:{Turn} - TABLE:[{Table}] - My Tiles:[{YourTiles}] - My Move:({Tile}, {Position})",
            playerOptions.Value.Name,
            turnNumber.HasValue ? (object)turnNumber.Value : "unavailable",
            string.Join(", ", turn.Table),
            string.Join(", ", turn.YourTiles),
            move.Tile,
            move.Position);
    }

    public void Record(bool win, IReadOnlyList<int>? finalTiles = null)
    {
        if (win)
        {
            logger.LogInformation("[{PlayerName}] I win!!", playerOptions.Value.Name);
        }
        else
        {
            logger.LogInformation("[{PlayerName}] I did not win.", playerOptions.Value.Name);
        }

        if (finalTiles is null)
        {
            logger.LogInformation("[{PlayerName}] Final Tiles: unavailable", playerOptions.Value.Name);
        }
        else
        {
            logger.LogInformation("[{PlayerName}] Final Tiles:[{FinalTiles}]",
                playerOptions.Value.Name, string.Join(", ", finalTiles));
        }
    }
}