using MediatR;

namespace PitchReserve.Application.Features.Playgrounds.Commands.DeletePlayground;

public sealed record DeletePlaygroundCommand(Guid Id) : IRequest;
