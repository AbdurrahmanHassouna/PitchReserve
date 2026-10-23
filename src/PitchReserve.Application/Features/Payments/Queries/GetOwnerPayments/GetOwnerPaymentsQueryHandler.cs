using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Payments.Queries.GetOwnerPayments;

public class GetOwnerPaymentsQueryHandler : IRequestHandler<GetOwnerPaymentsQuery, OwnerPaymentsSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOwnerPaymentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OwnerPaymentsSummaryDto> Handle(GetOwnerPaymentsQuery request, CancellationToken cancellationToken)
    {

        if (!request.OwnerUserId.HasValue || request.OwnerUserId != _currentUserService.UserId)
        {
            throw new ForbiddenAccessException("User must be authenticated to retrieve owner payments.");
        }

        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.Booking.Playground.OwnerProfile.UserId == request.OwnerUserId);

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= request.FromDate);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= request.ToDate);
        }

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new OwnerPaymentDto
            {
                PaymentId = p.Id,
                BookingId = p.BookingId,
                PlaygroundName = p.Booking.Playground.Name,
                PlayerName = p.Booking.OrganizerPlayer.FullName,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                PaidAt = p.PaidAt,
                TransactionReference = p.TransactionReference,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var totalRevenue = payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .Sum(p => p.Amount);

        var pendingCashAmount = payments
            .Where(p => p.Status == PaymentStatus.Pending && p.Method == PaymentMethod.CashOnArrival)
            .Sum(p => p.Amount);

        return new OwnerPaymentsSummaryDto
        {
            TotalRevenue = totalRevenue,
            PendingCashAmount = pendingCashAmount,
            TotalTransactions = payments.Count,
            SuccessfulTransactions = payments.Count(p => p.Status == PaymentStatus.Succeeded),
            PendingTransactions = payments.Count(p => p.Status == PaymentStatus.Pending),
            Payments = payments
        };
    }
}
