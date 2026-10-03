using FluentValidation;

namespace PitchReserve.Application.Features.Playgrounds.Commands.CreatePlayground;

public class CreatePlaygroundCommandValidator : AbstractValidator<CreatePlaygroundCommand>
{
    public CreatePlaygroundCommandValidator()
    {
        RuleFor(x => x.OwnerProfileId)
            .NotEmpty().WithMessage("Owner id is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Playground name is required.")
            .MaximumLength(150).WithMessage("Playground name must not exceed 150 characters.");

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0).WithMessage("Hourly rate must be greater than zero.");

        RuleFor(x => x.LocationCity)
            .NotEmpty().WithMessage("Location city is required.")
            .MaximumLength(100).WithMessage("Location city must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(250).WithMessage("Address must not exceed 250 characters.");

        RuleFor(x => x.Size)
            .IsInEnum().WithMessage("A valid pitch size must be specified.");

        RuleFor(x => x.SurfaceType)
            .IsInEnum().WithMessage("A valid surface type must be specified.");

        RuleFor(x => x.CloseHour)
            .Must((cmd, closeHour) => closeHour != cmd.OpenHour)
            .WithMessage("Close hour must not be equal to open hour.");
    }
}
