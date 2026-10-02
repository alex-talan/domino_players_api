using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApi.Contracts;

public sealed class EndGameHttpRequest
{
    [JsonRequired]
    [JsonPropertyName("win")]
    [Required]
    public bool? Win { get; init; }
}