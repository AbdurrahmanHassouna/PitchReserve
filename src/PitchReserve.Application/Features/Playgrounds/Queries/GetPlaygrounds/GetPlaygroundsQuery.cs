using MediatR;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygrounds;

public record GetPlaygroundsQuery : IRequest<IList<PlaygroundSummaryDto>>;