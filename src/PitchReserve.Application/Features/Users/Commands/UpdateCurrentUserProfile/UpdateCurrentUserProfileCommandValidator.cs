using FluentValidation;
using PitchReserve.Application.Common.Validation;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public sealed class UpdateCurrentUserProfileCommandValidator : AbstractValidator<UpdateCurrentUserProfileCommand>
{
    public UpdateCurrentUserProfileCommandValidator()
    {
        RuleFor(command => command)
            .Must(command => command.FullName is not null || command.PhoneNumber is not null)
            .WithMessage("Provide at least one profile field to update.");

        RuleFor(command => command.FullName)
            .Must(fullName => !string.IsNullOrWhiteSpace(fullName))
            .WithMessage("Full name cannot be blank.")
            .When(command => command.FullName is not null);

        RuleFor(command => command.FullName)
            .MaximumLength(100)
            .WithMessage("Full name cannot exceed 100 characters.")
            .When(command => command.FullName is not null);

        RuleFor(command => command.PhoneNumber)
            .Must(PhoneNumberRules.IsValid)
            .WithMessage("Provide an Egyptian mobile number, such as 01012345678 or +201012345678.")
            .When(command => command.PhoneNumber is not null);
    }
}
