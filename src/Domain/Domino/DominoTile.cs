namespace Domain.Domino;

public readonly record struct DominoTile(int Id, int Left, int Right)
{
    public int Points => Left + Right;

    public bool Matches(int end) => Left == end || Right == end;

    public int OtherEnd(int matchingEnd) => Left == matchingEnd ? Right : Left;
}