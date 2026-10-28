using MediatR;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Playgrounds.Queries.GetPlaygrounds;

namespace PitchReserve.Application.Features.Playgrounds.Queries.GetOwnerPlaygrounds;

public sealed record GetOwnerPlaygroundsQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PlaygroundSummaryDto>>;
