using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Bookings.Commands.RejoinOrganizer;

public record RejoinOrganizerCommand(Guid BookingId) : IRequest<Result>;
