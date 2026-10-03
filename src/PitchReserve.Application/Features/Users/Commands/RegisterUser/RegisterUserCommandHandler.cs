using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Common;
using PitchReserve.Domain.Entities;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = User.CreateWithPassword(
            request.FullName,
            normalizedEmail,
            request.Role,
            request.PhoneNumber,
            passwordHash);

        _context.Users.Add(user);

        Guid? ownerProfileId = null;

        if (request.Role == UserRole.Owner)
        {
            var ownerProfile = OwnerProfile.Create(
                user.Id,
                request.BusinessName!,
                request.TaxNumber);

            _context.OwnerProfiles.Add(ownerProfile);
            ownerProfileId = ownerProfile.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateJwtToken(
            user.Id.ToString(),
            user.Email,
            [ user.Role.ToString() ]);

        var response = new AuthResponseDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            token,
            ownerProfileId);

        return Result<AuthResponseDto>.Success(response);
    }
}
