using MediatR;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetJoinRequests;

public record GetJoinRequestsQuery : IRequest<PagedResult<JoinRequestDto>>
{
    public Guid BookingId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ParticipantStatus? Status { get; init; }
}
