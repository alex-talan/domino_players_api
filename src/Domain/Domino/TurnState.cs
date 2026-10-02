namespace Domain.Domino;

public sealed record TurnState(
    IReadOnlyList<int> Table,
    int? Head,
    int? Tail,
    IReadOnlyList<int> YourTiles);

public sealed record TileMove(int Tile, string Position)
{
    public static TileMove Pass { get; } = new(-1, string.Empty);
}