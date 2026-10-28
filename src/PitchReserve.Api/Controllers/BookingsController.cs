using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Bookings.Commands.CancelBooking;
using PitchReserve.Application.Features.Bookings.Commands.AcceptCashBooking;
using PitchReserve.Application.Features.Bookings.Commands.CreateBooking;
using PitchReserve.Application.Features.Bookings.Commands.CreateBookingByOwner;
using PitchReserve.Application.Features.Bookings.Commands.RejoinOrganizer;
using PitchReserve.Application.Features.Bookings.Commands.RequestToJoinMatch;
using PitchReserve.Application.Features.Bookings.Commands.RescheduleOwnerBooking;
using PitchReserve.Application.Features.Bookings.Commands.RespondToJoinRequest;
using PitchReserve.Application.Features.Bookings.Commands.UpdateBookingMatchSettings;
using PitchReserve.Application.Features.Bookings.Commands.WithdrawFromMatch;
using PitchReserve.Application.Features.Bookings.Queries.GetBookingById;
using PitchReserve.Application.Features.Bookings.Queries.GetJoinRequests;
using PitchReserve.Application.Features.Bookings.Queries.GetMyBookings;
using PitchReserve.Application.Features.Bookings.Queries.GetOwnerBookings;
using PitchReserve.Application.Features.Bookings.Queries.GetPublicMatches;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Api.Controllers;

[Authorize]
public class BookingsController : ApiControllerBase
{
    [Authorize(Roles = "Player")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost("owner")]
    public async Task<ActionResult<Guid>> CreateBookingByOwner([FromBody] CreateBookingByOwnerDto dto, CancellationToken cancellationToken)
    {
        var command = new CreateBookingByOwnerCommand
        {
            PlaygroundId = dto.PlaygroundId,
            OrganizerPlayerId = dto.OrganizerPlayerId,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            PaymentMethod = dto.PaymentMethod,
            IsOpenForPublicPlayers = dto.IsOpenForPublicPlayers,
            MaxPlayersNeeded = dto.MaxPlayersNeeded
        };
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Owner accepts the cash booking request; collection later confirms the booking.</summary>
    [Authorize(Roles = "Owner")]
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<Result>> AcceptCashBooking(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new AcceptCashBookingCommand(id), cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpGet("owner")]
    public async Task<ActionResult<List<OwnerBookingDto>>> GetOwnerBookings([FromQuery] GetOwnerBookingsQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [Authorize(Roles = "Player")]
    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<MyBookingDto>>> GetMyBookings([FromQuery] GetMyBookingsQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("public")]
    public async Task<ActionResult<PagedResult<PublicMatchDto>>> GetPublicMatches([FromQuery] GetPublicMatchesQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetBookingByIdQuery(id), cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:guid}/schedule")]
    public async Task<ActionResult<Result>> Reschedule(Guid id, [FromBody] RescheduleBookingDto dto, CancellationToken cancellationToken)
    {
        var command = new RescheduleOwnerBookingCommand { BookingId = id, StartTime = dto.StartTime, EndTime = dto.EndTime };
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [Authorize(Roles = "Player")]
    [HttpPut("{id:guid}/match-settings")]
    public async Task<ActionResult<Result>> UpdateMatchSettings(
        Guid id, [FromBody] UpdateBookingMatchSettingsDto dto, CancellationToken cancellationToken)
    {
        var command = new UpdateBookingMatchSettingsCommand
        {
            BookingId = id,
            IsOpenForPublicPlayers = dto.IsOpenForPublicPlayers,
            MaxPlayersNeeded = dto.MaxPlayersNeeded
        };
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<Result>> Cancel(Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelBookingDto? dto, CancellationToken cancellationToken)
    {
        var command = new CancelBookingCommand { BookingId = id, Reason = dto?.Reason };
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [Authorize(Roles = "Player")]
    [HttpPost("{id:guid}/join-requests")]
    public async Task<ActionResult<Result<Guid>>> RequestToJoinMatch(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new RequestToJoinMatchCommand(id), cancellationToken));

    [Authorize(Roles = "Player")]
    [HttpGet("{id:guid}/join-requests")]
    public async Task<ActionResult<PagedResult<JoinRequestDto>>> GetJoinRequests(Guid id,
        [FromQuery] GetJoinRequestsQuery query, CancellationToken cancellationToken)
    {
        var command = new GetJoinRequestsQuery { BookingId = id, Page = query.Page, PageSize = query.PageSize, Status = query.Status };
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    [Authorize(Roles = "Player")]
    [HttpPut("{id:guid}/join-requests/{participantId:guid}/respond")]
    public async Task<ActionResult<Result>> RespondToJoinRequest(Guid id, Guid participantId,
        [FromBody] RespondToJoinRequestDto dto, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new RespondToJoinRequestCommand(id, participantId, dto.Accept), cancellationToken));

    [Authorize(Roles = "Player")]
    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<Result>> Withdraw(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new WithdrawFromMatchCommand(id), cancellationToken));

    [Authorize(Roles = "Player")]
    [HttpPost("{id:guid}/rejoin")]
    public async Task<ActionResult<Result>> Rejoin(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new RejoinOrganizerCommand(id), cancellationToken));
}

public record CreateBookingByOwnerDto
{
    public Guid PlaygroundId { get; init; }
    public Guid OrganizerPlayerId { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public bool IsOpenForPublicPlayers { get; init; }
    public int? MaxPlayersNeeded { get; init; }
}

public record RescheduleBookingDto(DateTimeOffset StartTime, DateTimeOffset EndTime);
public record UpdateBookingMatchSettingsDto
{
    [Required]
    public bool? IsOpenForPublicPlayers { get; init; }

    public int? MaxPlayersNeeded { get; init; }
}
public record CancelBookingDto(string? Reason);
public record RespondToJoinRequestDto(bool Accept);
