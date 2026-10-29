using Application.Players;
using Domain.Domino;
using Infrastructure.Options;
using Infrastructure.Players;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PlayerOptions>()
            .Bind(configuration.GetSection(PlayerOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "Player:Name is required.")
            .Validate(options => options.Id is "p0" or "p1" or "p2" or "p3", "Player:Id must be p0, p1, p2, or p3.")
            .Validate(options => IsSupportedStrategy(options.Strategy), "Player:Strategy must be Greedy, Random, StrategyA, or StrategyB.")
            .ValidateOnStart();

        services.AddSingleton<ITileSelectionStrategy>(serviceProvider =>
            CreateStrategy(serviceProvider.GetRequiredService<IOptions<PlayerOptions>>().Value.Strategy));
        services.AddSingleton<IPlayerResultRecorder, LoggingPlayerResultRecorder>();
        services.AddScoped<PlayerApplicationService>();

        return services;
    }

    private static bool IsSupportedStrategy(string strategy) => strategy.ToUpperInvariant() is
        "GREEDY" or "RANDOM" or "STRATEGYA" or "STRATEGYB";

    private static ITileSelectionStrategy CreateStrategy(string strategy) => strategy.ToUpperInvariant() switch
    {
        "GREEDY" or "STRATEGYA" => new GreedyTileSelectionStrategy(),
        "RANDOM" or "STRATEGYB" => new RandomTileSelectionStrategy(),
        _ => throw new InvalidOperationException($"Player strategy '{strategy}' is not supported.")
    };
}
