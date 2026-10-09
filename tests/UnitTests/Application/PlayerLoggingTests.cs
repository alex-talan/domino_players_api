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
    [InlineData(19, "head", "[Player1] TABLE:[22, 4, 2] - My Tiles:[14, 19] - My Move:(19, head)")]
    [InlineData(-1, "", "[Player1] TABLE:[22, 4, 2] - My Tiles:[14, 19] - My Move:(-1, )")]
    public void RecordMove_ShouldLogReadableTileIdentifiers(int tile, string position, string expectedMessage)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));

        recorder.RecordMove(new TurnState([22, 4, 2], 4, 2, [14, 19]), new TileMove(tile, position));

        logger.Messages.Should().ContainSingle().Which.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(true, "[Player1] I win!!")]
    [InlineData(false, "[Player1] I did not win.")]
    public void RecordResult_ShouldLogTheSuppliedOutcomeWithoutBlockingLaterMoves(bool win, string expectedMessage)
    {
        CapturingLogger logger = new();
        LoggingPlayerResultRecorder recorder = new(logger, Options.Create(new PlayerOptions { Name = "Player1" }));
        PlayerApplicationService service = new(new RandomTileSelectionStrategy(), recorder);

        service.RecordResult(win);
        service.RecordResult(win);
        TileMove move = service.SelectMove(new TurnState([], null, null, [0]));

        logger.Messages.Take(2).Should().Equal(expectedMessage, expectedMessage);
        move.Should().Be(new TileMove(0, "head"));
        logger.Messages[2].Should().Be("[Player1] TABLE:[] - My Tiles:[0] - My Move:(0, head)");
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