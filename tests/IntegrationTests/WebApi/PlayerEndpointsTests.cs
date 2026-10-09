using System.Net;
using System.Net.Http.Json;
using System.Text;
using Application.Players;
using Domain.Domino;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.WebApi;

public sealed class PlayerEndpointsTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Play_ShouldSelectHighestScoringTile_OnFirstTurn()
    {
        HttpClient client = factory.CreateClient();
        object request = CreatePlayRequest([], null, null, [3, 14, 22, 0, 7, 19, 11]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PlayResponse>()).Should().Be(new PlayResponse(22, "head"));
    }

    [Fact]
    public async Task Play_ShouldChooseHighestScoringLegalTile_OnSubsequentTurn()
    {
        HttpClient client = factory.CreateClient();
        object request = CreatePlayRequest([22, 4, 2], 4, 2, [3, 14, 0, 7, 19, 11]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PlayResponse>()).Should().Be(new PlayResponse(19, "head"));
    }

    [Fact]
    public async Task Play_ShouldReturnPass_WhenNoHandTileMatchesEitherEnd()
    {
        HttpClient client = factory.CreateClient();
        object request = CreatePlayRequest([3], 0, 1, [22, 27]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PlayResponse>()).Should().Be(new PlayResponse(-1, string.Empty));
    }

    [Fact]
    public async Task Play_ShouldUseConfiguredRandomStrategy()
    {
        using TestWebApplicationFactory randomFactory = new() { Strategy = "StrategyB" };
        randomFactory.Services.GetRequiredService<IConfiguration>()["Player:Strategy"].Should().Be("StrategyB");
        HttpClient client = randomFactory.CreateClient();
        object request = CreatePlayRequest([22], 4, 2, [14, 19]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PlayResponse>()).Should().Be(new PlayResponse(14, "tail"));
    }

    [Fact]
    public async Task Play_ShouldAcceptNullHistoryForAnotherPlayer()
    {
        HttpClient client = factory.CreateClient();
        object request = CreatePlayRequest([], null, null, [22], nullP1: true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(26)]
    public async Task Play_ShouldForwardTheSuppliedTurnNumberToTheRecorder(int? turnNumber)
    {
        CapturingPlayerRecorder recorder = new();
        using Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> numberedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IPlayerResultRecorder>(recorder)));
        HttpClient client = numberedFactory.CreateClient();
        object request = CreatePlayRequest([], null, null, [22], turnNumber: turnNumber);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/play", request);
        HttpResponseMessage repeatedResponse = await client.PostAsJsonAsync("/play", request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        repeatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.TurnNumbers.Should().Equal(turnNumber, turnNumber);
    }

    [Theory]
    [MemberData(nameof(InvalidPlayRequests))]
    public async Task Play_ShouldReturnBadRequest_WhenRequestIsInvalid(object request)
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/play", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task End_ShouldAcceptWinnerNotification(bool win)
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/end", new { win });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task End_ShouldReturnBadRequest_WhenWinIsMissing()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/end", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{\"win\":true,\"your_tiles\":[]}")]
    [InlineData("{\"win\":false,\"your_tiles\":[0,14,19]}")]
    [InlineData("{\"win\":false,\"your_tiles\":null}")]
    public async Task End_ShouldAcceptOptionalFinalHand(string json)
    {
        HttpClient client = factory.CreateClient();
        using StringContent content = new(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/end", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("[-1]")]
    [InlineData("[28]")]
    [InlineData("[0,0]")]
    [InlineData("[0,1,2,3,4,5,6,7]")]
    [InlineData("[1.5]")]
    [InlineData("\"not an array\"")]
    public async Task End_ShouldRejectMalformedFinalHand(string handJson)
    {
        HttpClient client = factory.CreateClient();
        using StringContent content = new($"{{\"win\":false,\"your_tiles\":{handJson}}}", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/end", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    public static IEnumerable<object[]> InvalidPlayRequests()
    {
        yield return [CreatePlayRequest([], null, null, [])];
        yield return [CreatePlayRequest([], null, null, [28])];
        yield return [CreatePlayRequest([], null, null, [0, 0])];
        yield return [CreatePlayRequest([0, 0], 0, 0, [1])];
        yield return [CreatePlayRequest([0], 0, 1, [0])];
        yield return [CreatePlayRequest([0], 7, 1, [1])];
        yield return [CreatePlayRequest([], null, null, [1], nullP0: true)];
        yield return [CreatePlayRequest([], null, null, [1], toPlay: "p4")];
        yield return [CreatePlayRequest([], null, null, [1], turnNumber: 0)];
        yield return [CreatePlayRequest([], null, null, [1], turnNumber: -1)];
    }

    private static object CreatePlayRequest(
        int[] table,
        int? head,
        int? tail,
        int[] yourTiles,
        int[]? p0 = null,
        string toPlay = "p0",
        bool nullP0 = false,
        bool nullP1 = false,
        int? turnNumber = null) => new
        {
            table,
            head,
            tail,
            p0 = nullP0 ? null : p0 ?? [],
            p1 = nullP1 ? null : (int[]?)[],
            p2 = (int[]?)[],
            p3 = (int[]?)[],
            to_play = toPlay,
            turn = turnNumber,
            your_tiles = yourTiles
        };

    private sealed class CapturingPlayerRecorder : IPlayerResultRecorder
    {
        public List<int?> TurnNumbers { get; } = [];

        public void RecordMove(TurnState turn, TileMove move, int? turnNumber = null) => TurnNumbers.Add(turnNumber);

        public void Record(bool win, IReadOnlyList<int>? finalTiles = null)
        {
        }
    }

    private sealed record PlayResponse(int Tile, string Position);
}