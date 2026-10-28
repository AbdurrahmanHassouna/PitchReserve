using MediatR;
using PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygroundById;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygroundById;

public sealed record GetOwnerPlaygroundByIdQuery(Guid Id) : IRequest<PlaygroundDto>;
