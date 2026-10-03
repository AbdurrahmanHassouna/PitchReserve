namespace PitchReserve.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Bookings.Commands.CreateBooking;
using PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;
using PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;
using PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;

[Authorize]
public class BookingsController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public BookingsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBookingCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = "Owner")]
    [HttpGet("owner")]
    [ProducesResponseType(typeof(List<OwnerBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OwnerBookingDto>>> GetOwnerBookings([FromQuery] GetOwnerBookingsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/join-requests")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Result<Guid>>> RequestToJoinMatch(Guid id)
    {
        var playerId = _currentUserService.UserId;
        if (!playerId.HasValue)
        {
            return Unauthorized();
        }

        var command = new RequestToJoinMatchCommand(id, playerId.Value);
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("{id:guid}/join-requests/{participantId:guid}/respond")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> RespondToJoinRequest(
        Guid id,
        Guid participantId,
        [FromBody] RespondToJoinRequestDto dto)
    {
        var command = new RespondToJoinRequestCommand(id, participantId, dto.Accept);
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}

public record RespondToJoinRequestDto
{
    public bool Accept { get; init; }
}
