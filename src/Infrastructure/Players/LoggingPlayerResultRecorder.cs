using Application.Players;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Players;

public sealed class LoggingPlayerResultRecorder(
    ILogger<LoggingPlayerResultRecorder> logger,
    IOptions<PlayerOptions> playerOptions) : IPlayerResultRecorder
{
    public void Record(bool win)
    {
        logger.LogInformation("Player {PlayerName} received game result: {Win}", playerOptions.Value.Name, win);
    }
}