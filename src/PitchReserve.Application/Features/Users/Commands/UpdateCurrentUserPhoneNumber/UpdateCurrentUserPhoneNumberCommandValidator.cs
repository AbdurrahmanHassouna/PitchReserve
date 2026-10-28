using FluentValidation;
using PitchReserve.Application.Common.Validation;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserPhoneNumber;

public class UpdateCurrentUserPhoneNumberCommandValidator : AbstractValidator<UpdateCurrentUserPhoneNumberCommand>
{
    public UpdateCurrentUserPhoneNumberCommandValidator() => RuleFor(x => x.PhoneNumber)
        .Must(PhoneNumberRules.IsValid).WithMessage("Provide an Egyptian mobile number, such as 01012345678 or +201012345678.");
}
