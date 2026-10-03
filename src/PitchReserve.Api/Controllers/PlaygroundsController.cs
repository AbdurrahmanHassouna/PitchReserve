namespace PitchReserve.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Playgrounds.Commands.CreatePlayground;
using Application.Features.Playgrounds.Queries.GetPlaygroundById;
using Application.Features.Playgrounds.Queries.GetPlaygrounds;

public class PlaygroundsController : ApiControllerBase
{
    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePlaygroundCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlaygroundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaygroundDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetPlaygroundByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IList<PlaygroundSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<PlaygroundSummaryDto>>> Get()
    {
        var result = await Mediator.Send(new GetPlaygroundsQuery());
        return Ok(result);
    }
}
