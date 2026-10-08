using MediatR;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygroundById;

public record GetPlaygroundByIdQuery(Guid Id) : IRequest<PlaygroundDto>;
