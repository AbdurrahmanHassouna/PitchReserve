using MediatR;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetPublicMatches;

public record GetPublicMatchesQuery : IRequest<PagedResult<PublicMatchDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? PlaygroundId { get; init; }
    public BookingStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
