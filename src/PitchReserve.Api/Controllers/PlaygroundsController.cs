namespace PitchReserve.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Api.Contracts.Playgrounds;
using Application.Common.Models;
using Application.Features.Playgrounds.Commands.DeletePlayground;
using Application.Features.Playgrounds.Commands.UpdatePlayground;
using Application.Features.Playgrounds.Commands.CreatePlayground;
using Application.Features.Playgrounds.Queries.GetPlaygroundById;
using Application.Features.Playgrounds.Queries.GetOwnerPlaygroundById;
using Application.Features.Playgrounds.Queries.GetOwnerPlaygrounds;
using Application.Features.Playgrounds.Queries.GetPlaygrounds;

public class PlaygroundsController : ApiControllerBase
{
    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePlaygroundCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlaygroundDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetPlaygroundByIdQuery(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IList<PlaygroundSummaryDto>>> Get()
    {
        var result = await Mediator.Send(new GetPlaygroundsQuery());
        return Ok(result);
    }

    /// <summary>Lists the authenticated owner's playgrounds, including closed and inactive fields.</summary>
    [Authorize(Roles = "Owner")]
    [HttpGet("owner")]
    public async Task<ActionResult<PagedResult<PlaygroundSummaryDto>>> GetOwnerPlaygrounds(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetOwnerPlaygroundsQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>Returns an owner's field details, including inactive fields kept for history.</summary>
    [Authorize(Roles = "Owner")]
    [HttpGet("owner/{id:guid}")]
    public async Task<ActionResult<PlaygroundDto>> GetOwnerPlaygroundById(Guid id)
    {
        var result = await Mediator.Send(new GetOwnerPlaygroundByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Updates all editable field details. Changing opening hours cannot invalidate future active bookings.</summary>
    [Authorize(Roles = "Owner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlaygroundDto dto)
    {
        await Mediator.Send(new UpdatePlaygroundCommand(id, dto.Name, dto.Size, dto.SurfaceType,
            dto.HourlyRate, dto.LocationCity, dto.Address, dto.Latitude, dto.Longitude,
            dto.OpenHour, dto.CloseHour, dto.Status));
        return NoContent();
    }

    /// <summary>Deletes a field without booking history, or deactivates it when history must be preserved.</summary>
    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeletePlaygroundCommand(id));
        return NoContent();
    }
}
