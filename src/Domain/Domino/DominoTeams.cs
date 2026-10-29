namespace Domain.Domino;

public static class DominoTeams
{
    public static string GetTeamId(string playerId) => playerId switch
    {
        "p0" or "p2" => "Team0",
        "p1" or "p3" => "Team1",
        _ => throw new ArgumentException("Player identity must be p0, p1, p2, or p3.", nameof(playerId))
    };
}
