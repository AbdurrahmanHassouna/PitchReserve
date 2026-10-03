using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(v => v.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(v => v.PhoneNumber).MinimumLength(3)
            .WithMessage("Phone number must be at least 3 characters.");

        RuleFor(v => v.Role)
            .IsInEnum().Must(x=> x is UserRole.Player or UserRole.Owner).WithMessage("Role must be a valid user role.");
        When(v => v.Role == UserRole.Owner, () =>
        {
            RuleFor(v => v.BusinessName)
                .NotEmpty().WithMessage("Business name is required for Owners")
                .MaximumLength(150).WithMessage("Business name must not exceed 150 characters.");
        });
    }
}
