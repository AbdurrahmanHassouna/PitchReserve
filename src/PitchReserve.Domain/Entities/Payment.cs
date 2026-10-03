using PitchReserve.Domain.Common;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public Currency Currency { get; private set; }
    public string? StripeSessionId { get; private set; }
    public string? TransactionReference { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public Booking Booking { get; private set; } = null!;

    private Payment() { }

    public static Payment Create(
        Guid bookingId,
        decimal amount,
        PaymentMethod method,
        string? stripeSessionId = null)
    {
        if (bookingId == Guid.Empty)
            throw new DomainException("BookingId is required.");

        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        return new Payment
        {
            BookingId = bookingId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Pending,
            StripeSessionId = stripeSessionId
        };
    }

    public void MarkAsPaid(DateTime? paidAt = null)
    {
        if (Method is not PaymentMethod.CashOnArrival)
            throw new DomainException("Can't confirm Online Payments.");
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt ?? DateTime.UtcNow;
    }

    public void MarkAsFailed(string? failureReference = null)
    {
        Status = PaymentStatus.Failed;

        if (!string.IsNullOrWhiteSpace(failureReference))
        {
            TransactionReference = failureReference.Trim();
        }
    }
}
