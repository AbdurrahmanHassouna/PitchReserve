using FluentValidation;

namespace PitchReserve.Application.Features.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.PlaygroundId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Comment).MaximumLength(1000);
    }
}
