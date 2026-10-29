namespace Infrastructure.Options;

public sealed class PlayerOptions
{
    public const string SectionName = "Player";

    public string Name { get; init; } = "Player";

    public string Id { get; init; } = "p0";

    public string Strategy { get; init; } = "Greedy";
}