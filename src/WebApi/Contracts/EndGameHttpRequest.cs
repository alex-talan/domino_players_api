using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApi.Contracts;

public sealed class EndGameHttpRequest : IValidatableObject
{
    [JsonRequired]
    [JsonPropertyName("win")]
    [Required]
    public bool? Win { get; init; }

    [JsonPropertyName("your_tiles")]
    public int[]? YourTiles { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (YourTiles is null)
        {
            yield break;
        }

        if (YourTiles.Length > 7 || YourTiles.Any(tile => tile is < 0 or > 27)
            || YourTiles.Distinct().Count() != YourTiles.Length)
        {
            yield return new ValidationResult(
                "Final hand must contain zero to seven unique tile identifiers from 0 to 27.", [nameof(YourTiles)]);
        }
    }
}