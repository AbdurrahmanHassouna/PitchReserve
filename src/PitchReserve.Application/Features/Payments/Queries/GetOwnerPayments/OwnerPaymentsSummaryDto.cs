using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Payments.Queries.GetOwnerPayments;

public record OwnerPaymentDto
{
    public Guid PaymentId { get; init; }
    public Guid BookingId { get; init; }
    public string PlaygroundName { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? TransactionReference { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OwnerPaymentsSummaryDto
{
    public decimal TotalRevenue { get; init; }
    public decimal PendingCashAmount { get; init; }
    public int TotalTransactions { get; init; }
    public int SuccessfulTransactions { get; init; }
    public int PendingTransactions { get; init; }
    public IReadOnlyList<OwnerPaymentDto> Payments { get; init; } = Array.Empty<OwnerPaymentDto>();
}
