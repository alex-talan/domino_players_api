using Application.Players;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;

namespace WebApi.Controllers;

[ApiController]
[Route("")]
public sealed class PlayerController(PlayerApplicationService playerApplicationService) : ControllerBase
{
    [HttpPost("play")]
    [ProducesResponseType<PlayTurnHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<PlayTurnHttpResponse> Play(PlayTurnHttpRequest request)
    {
        Domain.Domino.TileMove move = playerApplicationService.SelectMove(request.ToTurnState());
        return Ok(new PlayTurnHttpResponse(move.Tile, move.Position));
    }

    [HttpPost("end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult End(EndGameHttpRequest request)
    {
        playerApplicationService.RecordResult(request.Win!.Value);
        return Ok();
    }
}