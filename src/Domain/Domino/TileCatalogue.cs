namespace Domain.Domino;

public static class TileCatalogue
{
    public static IReadOnlyList<DominoTile> All { get; } = Array.AsReadOnly<DominoTile>(
    [
        new(0, 0, 0), new(1, 0, 1), new(2, 0, 2), new(3, 0, 3), new(4, 0, 4), new(5, 0, 5), new(6, 0, 6),
        new(7, 1, 1), new(8, 1, 2), new(9, 1, 3), new(10, 1, 4), new(11, 1, 5), new(12, 1, 6),
        new(13, 2, 2), new(14, 2, 3), new(15, 2, 4), new(16, 2, 5), new(17, 2, 6),
        new(18, 3, 3), new(19, 3, 4), new(20, 3, 5), new(21, 3, 6),
        new(22, 4, 4), new(23, 4, 5), new(24, 4, 6),
        new(25, 5, 5), new(26, 5, 6),
        new(27, 6, 6)
    ]);

    public static DominoTile GetById(int id) => All[id];
}