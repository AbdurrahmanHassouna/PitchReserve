using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.WithdrawFromMatch;

public record WithdrawFromMatchCommand(Guid BookingId) : IRequest<Result>;
