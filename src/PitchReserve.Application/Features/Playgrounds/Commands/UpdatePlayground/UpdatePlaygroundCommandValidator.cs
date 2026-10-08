using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Playgrounds.Commands.UpdatePlayground;

public sealed class UpdatePlaygroundCommandValidator : AbstractValidator<UpdatePlaygroundCommand>
{
    public UpdatePlaygroundCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Size).IsInEnum();
        RuleFor(command => command.SurfaceType).IsInEnum();
        RuleFor(command => command.HourlyRate).GreaterThan(0);
        RuleFor(command => command.LocationCity).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Address).NotEmpty().MaximumLength(250);
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.CloseHour)
            .Must((command, close) => close != command.OpenHour)
            .WithMessage("Open and close hours must be different.");
        RuleFor(command => command.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(command => command.Latitude.HasValue);
        RuleFor(command => command.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(command => command.Longitude.HasValue);
    }
}
