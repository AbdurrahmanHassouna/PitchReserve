using FluentValidation;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Bookings.Queries.GetJoinRequests;

public class GetJoinRequestsQueryValidator : AbstractValidator<GetJoinRequestsQuery>
{
    public GetJoinRequestsQueryValidator()
    {
        RuleFor(query => query.BookingId).NotEmpty();
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status is invalid.");
    }
}
