using Domain.Domino;
using FluentAssertions;

namespace UnitTests.Domain.Domino;

public sealed class TileSelectionStrategyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Strategies_ShouldOpenWithDoubleSixRegardlessOfHandOrder(bool random)
    {
        ITileSelectionStrategy strategy = random ? new RandomTileSelectionStrategy() : new GreedyTileSelectionStrategy();
        TurnState turn = new([], null, null, [0, 14, 27, 22]);

        strategy.SelectMove(turn).Should().Be(new TileMove(27, "head"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Strategies_ShouldRejectAnOpeningHandWithoutDoubleSix(bool random)
    {
        ITileSelectionStrategy strategy = random ? new RandomTileSelectionStrategy() : new GreedyTileSelectionStrategy();
        Action action = () => strategy.SelectMove(new TurnState([], null, null, [0, 22]));

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("p0", "Team0")]
    [InlineData("p1", "Team1")]
    [InlineData("p2", "Team0")]
    [InlineData("p3", "Team1")]
    public void Teams_ShouldPairOppositeSeats(string playerId, string teamId)
    {
        DominoTeams.GetTeamId(playerId).Should().Be(teamId);
    }

    [Fact]
    public void Teams_ShouldRejectUnknownPlayers()
    {
        Action action = () => DominoTeams.GetTeamId("p4");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Greedy_ShouldSelectHighestScoringPlayableTile()
    {
        TurnState turn = new([22, 4, 2], 4, 2, [14, 19]);

        TileMove move = new GreedyTileSelectionStrategy().SelectMove(turn);

        move.Should().Be(new TileMove(19, "head"));
    }

    [Fact]
    public void Greedy_ShouldIgnoreHigherScoringTileThatDoesNotMatchEitherEnd()
    {
        TurnState turn = new([1], 2, 5, [24, 5, 23]);

        TileMove move = new GreedyTileSelectionStrategy().SelectMove(turn);

        move.Tile.Should().Be(23);
        move.Position.Should().Be("tail");
    }

    [Fact]
    public void Greedy_ShouldPreferTheEndWithMoreCompatibleRemainingTiles()
    {
        TurnState turn = new([26], 5, 6, [26, 25]);

        TileMove move = new GreedyTileSelectionStrategy().SelectMove(turn);

        move.Should().Be(new TileMove(26, "tail"));
    }

    [Fact]
    public void Greedy_ShouldRandomizeBetweenEqualHighestScores()
    {
        TurnState turn = new([5], 0, 5, [6, 11]);

        TileMove move = new GreedyTileSelectionStrategy().SelectMove(turn);

        move.Tile.Should().BeOneOf(6, 11);
        TileCatalogue.GetById(move.Tile).Points.Should().Be(6);
    }

    [Fact]
    public void Greedy_ShouldPassWhenNoTileIsPlayable()
    {
        TurnState turn = new([3], 0, 1, [22, 27]);

        TileMove move = new GreedyTileSelectionStrategy().SelectMove(turn);

        move.Should().Be(TileMove.Pass);
    }

    [Fact]
    public void Random_ShouldSelectTheFirstPlayableTileInHandOrder()
    {
        TurnState turn = new([22], 4, 2, [27, 19, 14]);

        TileMove move = new RandomTileSelectionStrategy().SelectMove(turn);

        move.Should().Be(new TileMove(19, "head"));
    }

    [Fact]
    public void Random_ShouldPlayTileZeroInsteadOfPassing()
    {
        TurnState turn = new([1], 0, 1, [0]);

        TileMove move = new RandomTileSelectionStrategy().SelectMove(turn);

        move.Should().Be(new TileMove(0, "head"));
    }

    [Fact]
    public void Catalogue_ShouldContainTwentyEightTilesInIdentifierOrder()
    {
        TileCatalogue.All.Should().HaveCount(28);
        TileCatalogue.All.Select(tile => tile.Id).Should().Equal(Enumerable.Range(0, 28));
        TileCatalogue.GetById(22).Points.Should().Be(8);
    }
}