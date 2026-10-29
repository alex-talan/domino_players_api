using Application.Players;
using Domain.Domino;
using FluentAssertions;
using Infrastructure.Options;
using Infrastructure.Players;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Application;

public sealed class PlayerLoggingTests
{
    [Theory]
    [InlineData(19, "head")]
    [InlineData(-1, "")]
    public void SelectMove_ShouldRecordTheSuppliedStateAndSelectedMove(int tile, string position)
    {
        ITileSelectionStrategy strategy = Substitute.For<ITileSelectionStrategy>();
        IPlayerResultRecorder recorder = Substitute.For<IPlayerResultRecorder>();
        TurnState turn = new([22, 4, 2], 4, 2, [14, 19]);
        TileMove move = new(tile, position);
        strategy.SelectMove(turn).Returns(move);
        PlayerApplicationService service = new(strategy, recorder);

        service.SelectMove(turn).Should().Be(move);

        recorder.Received(1).RecordMove(turn, move);
    }

    [Theory]
    [InlineData(19, "head", "[Player1] [p0/Team0] TURN:unavailable - TABLE:[22, 4, 2] - My Tiles:[14, 19] - My Move:(19, head)")]
    [InlineData(-1, "", "[Player1] [p0/Team0] TURN:unavailable - TABLE:[22, 4, 2] - My Tiles:[14, 19] - My Move:(-1, )")]
    public void RecordMove_ShouldLogReadableTileIdentifiers(int tile, string position, string expectedMessage)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));

        recorder.RecordMove(new TurnState([22, 4, 2], 4, 2, [14, 19]), new TileMove(tile, position));

        logger.Messages.Should().ContainSingle().Which.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(true, "[Player1] [p0/Team0] Winning team: Team0. My team wins.")]
    [InlineData(false, "[Player1] [p0/Team0] Winning team: Team1. My team loses.")]
    public void RecordResult_ShouldLogTheSuppliedOutcomeWithoutBlockingLaterMoves(bool win, string expectedMessage)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));
        PlayerApplicationService service = new(new RandomTileSelectionStrategy(), recorder);

        service.RecordResult(win);
        service.RecordResult(win);
        TileMove move = service.SelectMove(new TurnState([], null, null, [0, 27]));

        logger.Messages.Should().Equal(expectedMessage, "[Player1] [p0/Team0] Final Tiles: unavailable",
            expectedMessage, "[Player1] [p0/Team0] Final Tiles: unavailable",
            "[Player1] [p0/Team0] TURN:unavailable - TABLE:[] - My Tiles:[0, 27] - My Move:(27, head)");
        move.Should().Be(new TileMove(27, "head"));
    }

    [Theory]
    [InlineData(true, new int[] { }, "[Player1] [p0/Team0] Final Tiles:[]")]
    [InlineData(true, new int[] { 14, 19 }, "[Player1] [p0/Team0] Final Tiles:[14, 19]")]
    [InlineData(false, new int[] { 0, 14, 19 }, "[Player1] [p0/Team0] Final Tiles:[0, 14, 19]")]
    public void RecordResult_ShouldLogTheExactFinalHand(bool win, int[] finalTiles, string expectedMessage)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));
        PlayerApplicationService service = new(new RandomTileSelectionStrategy(), recorder);

        service.RecordResult(win, finalTiles);

        logger.Messages.Should().HaveCount(2);
        logger.Messages[1].Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(25, -1, "")]
    [InlineData(26, 3, "head")]
    public void SelectMove_ShouldLogTheMastersTurnNumberIncludingPasses(int turnNumber, int tile, string position)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));
        ITileSelectionStrategy strategy = Substitute.For<ITileSelectionStrategy>();
        TurnState turn = new([14], 3, 1, [3]);
        strategy.SelectMove(turn).Returns(new TileMove(tile, position));
        PlayerApplicationService service = new(strategy, recorder);

        service.SelectMove(turn, turnNumber);
        service.SelectMove(turn, turnNumber);

        string expected = $"[Player1] [p0/Team0] TURN:{turnNumber} - TABLE:[14] - My Tiles:[3] - My Move:({tile}, {position})";
        logger.Messages.Should().Equal(expected, expected);
    }

    [Theory]
    [InlineData("p0", true, "Team0", "Team0", "wins")]
    [InlineData("p2", true, "Team0", "Team0", "wins")]
    [InlineData("p1", false, "Team1", "Team0", "loses")]
    [InlineData("p3", false, "Team1", "Team0", "loses")]
    [InlineData("p0", false, "Team0", "Team1", "loses")]
    [InlineData("p2", false, "Team0", "Team1", "loses")]
    [InlineData("p1", true, "Team1", "Team1", "wins")]
    [InlineData("p3", true, "Team1", "Team1", "wins")]
    public void RecordResult_ShouldIdentifyTheWinningTeamForEverySeat(
        string playerId, bool win, string teamId, string winningTeamId, string outcome)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger,
            Options.Create(new PlayerOptions { Name = "TestPlayer", Id = playerId }));

        recorder.Record(win, [14]);

        logger.Messages[0].Should().Be(
            $"[TestPlayer] [{playerId}/{teamId}] Winning team: {winningTeamId}. My team {outcome}.");
    }

    private sealed class CapturingLogger : ILogger<LoggingPlayerResultRecorder>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            logLevel.Should().Be(LogLevel.Information);
            Messages.Add(formatter(state, exception));
        }
    }
}