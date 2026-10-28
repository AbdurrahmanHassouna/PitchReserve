using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserPhoneNumber;

public sealed class UpdateCurrentUserPhoneNumberCommandHandler : IRequestHandler<UpdateCurrentUserPhoneNumberCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateCurrentUserPhoneNumberCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateCurrentUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Authentication is required.");
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId.Value);
        user.UpdatePhoneNumber(request.PhoneNumber);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
