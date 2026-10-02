using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Domino;

namespace WebApi.Contracts;

public sealed class PlayTurnHttpRequest : IValidatableObject
{
    [JsonRequired]
    [JsonPropertyName("table")]
    [Required]
    public int[]? Table { get; init; }

    [JsonRequired]
    [JsonPropertyName("head")]
    public int? Head { get; init; }

    [JsonRequired]
    [JsonPropertyName("tail")]
    public int? Tail { get; init; }

    [JsonRequired]
    [JsonPropertyName("p0")]
    public int[]? P0 { get; init; }

    [JsonRequired]
    [JsonPropertyName("p1")]
    public int[]? P1 { get; init; }

    [JsonRequired]
    [JsonPropertyName("p2")]
    public int[]? P2 { get; init; }

    [JsonRequired]
    [JsonPropertyName("p3")]
    public int[]? P3 { get; init; }

    [JsonRequired]
    [JsonPropertyName("to_play")]
    [Required]
    public string? ToPlay { get; init; }

    [JsonRequired]
    [JsonPropertyName("your_tiles")]
    [Required]
    public int[]? YourTiles { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Table is not null)
        {
            foreach (ValidationResult result in ValidateTileIds(Table, nameof(Table), allowPass: false))
            {
                yield return result;
            }

            if (Table.Distinct().Count() != Table.Length)
            {
                yield return new ValidationResult("Table cannot contain duplicate tile identifiers.", [nameof(Table)]);
            }

            if (Table.Length == 0)
            {
                if (Head is not null || Tail is not null)
                {
                    yield return new ValidationResult("Head and tail must be null for an empty table.", [nameof(Head), nameof(Tail)]);
                }
            }
            else if (Head is not (>= 0 and <= 6) || Tail is not (>= 0 and <= 6))
            {
                yield return new ValidationResult("Head and tail must be integers from 0 to 6 for a nonempty table.", [nameof(Head), nameof(Tail)]);
            }
        }

        if (YourTiles is not null)
        {
            foreach (ValidationResult result in ValidateTileIds(YourTiles, nameof(YourTiles), allowPass: false))
            {
                yield return result;
            }

            if (YourTiles.Length is < 1 or > 7)
            {
                yield return new ValidationResult("Your hand must contain between one and seven tiles.", [nameof(YourTiles)]);
            }

            if (YourTiles.Distinct().Count() != YourTiles.Length)
            {
                yield return new ValidationResult("Your hand cannot contain duplicate tile identifiers.", [nameof(YourTiles)]);
            }

            if (Table is not null && Table.Intersect(YourTiles).Any())
            {
                yield return new ValidationResult("Table and hand cannot contain the same tile identifier.", [nameof(Table), nameof(YourTiles)]);
            }
        }

        foreach ((string player, int[]? history) in GetHistories())
        {
            if (history is not null)
            {
                foreach (ValidationResult result in ValidateTileIds(history, player, allowPass: true))
                {
                    yield return result;
                }
            }
        }

        int[]? currentHistory = ToPlay switch
        {
            "p0" => P0,
            "p1" => P1,
            "p2" => P2,
            "p3" => P3,
            _ => null
        };

        if (ToPlay is not ("p0" or "p1" or "p2" or "p3"))
        {
            yield return new ValidationResult("to_play must be p0, p1, p2, or p3.", [nameof(ToPlay)]);
        }
        else if (currentHistory is null)
        {
            yield return new ValidationResult("The current player's history cannot be null.", [nameof(ToPlay)]);
        }
    }

    public TurnState ToTurnState() => new(Table!, Head, Tail, YourTiles!);

    private IEnumerable<(string Player, int[]? History)> GetHistories()
    {
        yield return (nameof(P0), P0);
        yield return (nameof(P1), P1);
        yield return (nameof(P2), P2);
        yield return (nameof(P3), P3);
    }

    private static IEnumerable<ValidationResult> ValidateTileIds(int[] tileIds, string propertyName, bool allowPass)
    {
        int minimum = allowPass ? -1 : 0;
        if (tileIds.Any(tileId => tileId < minimum || tileId > 27))
        {
            yield return new ValidationResult($"{propertyName} entries must be integers from {minimum} to 27.", [propertyName]);
        }
    }
}