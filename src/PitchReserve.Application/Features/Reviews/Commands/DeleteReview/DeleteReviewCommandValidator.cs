using FluentValidation;

namespace PitchReserve.Application.Features.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
