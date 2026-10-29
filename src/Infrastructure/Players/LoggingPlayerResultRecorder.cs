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
            "[{PlayerName}] [{PlayerId}/{TeamId}] TURN:{Turn} - TABLE:[{Table}] - My Tiles:[{YourTiles}] - My Move:({Tile}, {Position})",
            playerOptions.Value.Name,
            playerOptions.Value.Id,
            DominoTeams.GetTeamId(playerOptions.Value.Id),
            turnNumber.HasValue ? (object)turnNumber.Value : "unavailable",
            string.Join(", ", turn.Table),
            string.Join(", ", turn.YourTiles),
            move.Tile,
            move.Position);
    }

    public void Record(bool win, IReadOnlyList<int>? finalTiles = null)
    {
        string teamId = DominoTeams.GetTeamId(playerOptions.Value.Id);
        string winningTeamId = win ? teamId : teamId == "Team0" ? "Team1" : "Team0";
        logger.LogInformation(
            "[{PlayerName}] [{PlayerId}/{TeamId}] Winning team: {WinningTeamId}. My team {Outcome}.",
            playerOptions.Value.Name, playerOptions.Value.Id, teamId, winningTeamId, win ? "wins" : "loses");

        if (finalTiles is null)
        {
            logger.LogInformation("[{PlayerName}] [{PlayerId}/{TeamId}] Final Tiles: unavailable",
                playerOptions.Value.Name, playerOptions.Value.Id, teamId);
        }
        else
        {
            logger.LogInformation("[{PlayerName}] [{PlayerId}/{TeamId}] Final Tiles:[{FinalTiles}]",
                playerOptions.Value.Name, playerOptions.Value.Id, teamId, string.Join(", ", finalTiles));
        }
    }
}