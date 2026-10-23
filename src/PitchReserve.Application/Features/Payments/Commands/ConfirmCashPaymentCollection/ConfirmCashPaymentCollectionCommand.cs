using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Payments.Commands.ConfirmCashPaymentCollection;

public record ConfirmCashPaymentCollectionCommand : IRequest<Result>
{
    public Guid PaymentId { get; init; }
    public DateTime? PaidAt { get; init; }

    public ConfirmCashPaymentCollectionCommand() { }

    public ConfirmCashPaymentCollectionCommand(Guid paymentId, DateTime? paidAt)
    {
        PaymentId = paymentId;
        PaidAt = paidAt;

    }
}
