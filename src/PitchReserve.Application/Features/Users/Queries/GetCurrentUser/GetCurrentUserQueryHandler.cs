using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            throw new ForbiddenAccessException("User must be authenticated.");
        }

        var user = await _context.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), _currentUserService.UserId.Value);
        }

        var dto = new CurrentUserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.PhoneNumber,
            user.OwnerProfile?.Id,
            user.OwnerProfile?.BusinessName);

        return Result<CurrentUserDto>.Success(dto);
    }
}
