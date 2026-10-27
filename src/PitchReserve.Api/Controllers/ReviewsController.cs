using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PitchReserve.Api.Contracts.Reviews;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Reviews.Commands.CreateReview;
using PitchReserve.Application.Features.Reviews.Commands.DeleteReview;
using PitchReserve.Application.Features.Reviews.Queries.GetPlaygroundReviews;

namespace PitchReserve.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ReviewsController : ApiControllerBase
{

    /// <summary>Lists public reviews for a playground. Inactive fields keep their review history.</summary>
    [HttpGet("playgrounds/{playgroundId:guid}/reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetPlaygroundReviews(
        Guid playgroundId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPlaygroundReviewsQuery(playgroundId, page, pageSize));
        return Ok(result);
    }

    /// <summary>Creates one review after a completed booking or accepted match on the playground.</summary>
    [Authorize(Roles = "Player")]
    [HttpPost("playgrounds/{playgroundId:guid}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(Guid playgroundId, [FromBody] CreateReviewDto dto)
    {
        var result = await Mediator.Send(new CreateReviewCommand(playgroundId, dto.Rating, dto.Comment));
        return CreatedAtAction(nameof(GetPlaygroundReviews),
            new { playgroundId, page = 1, pageSize = 20 }, result);
    }

    /// <summary>Removes a review and recalculates the playground rating aggregate.</summary>
    [Authorize(Roles = "Player")]
    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        await Mediator.Send(new DeleteReviewCommand(id));
        return NoContent();
    }
}
