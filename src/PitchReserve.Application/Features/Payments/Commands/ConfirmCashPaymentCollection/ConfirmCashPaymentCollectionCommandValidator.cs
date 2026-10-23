using FluentValidation;

namespace PitchReserve.Application.Features.Payments.Commands.ConfirmCashPaymentCollection;

public class ConfirmCashPaymentCollectionCommandValidator : AbstractValidator<ConfirmCashPaymentCollectionCommand>
{
    public ConfirmCashPaymentCollectionCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty().WithMessage("PaymentId is required.");
    }
}
