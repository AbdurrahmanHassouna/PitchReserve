using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public sealed class UpdateCurrentUserProfileCommandHandler
    : IRequestHandler<UpdateCurrentUserProfileCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCurrentUserProfileCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateCurrentUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Authentication is required.");

        var userId = _currentUser.UserId.Value;
        var user = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var fullName = request.FullName ?? user.FullName;
        var phoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.UpdateProfile(fullName, phoneNumber);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
