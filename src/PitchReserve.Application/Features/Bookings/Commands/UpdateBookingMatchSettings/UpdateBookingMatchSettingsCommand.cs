using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.UpdateBookingMatchSettings;

public record UpdateBookingMatchSettingsCommand : IRequest<Result>
{
    public Guid BookingId { get; init; }
    public bool? IsOpenForPublicPlayers { get; init; }
    public int? MaxPlayersNeeded { get; init; }
}
