using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

public sealed class GetPlaygroundReviewsQueryHandler : IRequestHandler<GetPlaygroundReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlaygroundReviewsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedResult<ReviewDto>> Handle(GetPlaygroundReviewsQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Playgrounds.AsNoTracking()
                .AnyAsync(playground => playground.Id == request.PlaygroundId, cancellationToken))
            throw new NotFoundException(nameof(Playground), request.PlaygroundId);

        var query = _context.Reviews.AsNoTracking()
            .Where(review => review.PlaygroundId == request.PlaygroundId);
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min(((long)request.Page - 1) * request.PageSize, int.MaxValue);
        var items = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenBy(review => review.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(review => new ReviewDto
            {
                Id = review.Id,
                AuthorDisplayName = review.Player.FullName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(items, request.Page, request.PageSize, totalCount);
    }
}
