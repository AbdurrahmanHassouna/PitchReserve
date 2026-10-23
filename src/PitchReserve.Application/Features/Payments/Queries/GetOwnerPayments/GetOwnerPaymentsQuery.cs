using MediatR;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Payments.Queries.GetOwnerPayments;

public record GetOwnerPaymentsQuery : IRequest<OwnerPaymentsSummaryDto>
{
    public Guid? OwnerUserId { get; init; }
    public PaymentStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    public GetOwnerPaymentsQuery() { }

    public GetOwnerPaymentsQuery(Guid? ownerUserId, PaymentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        OwnerUserId = ownerUserId;
        Status = status;
        FromDate = fromDate;
        ToDate = toDate;
    }
}
