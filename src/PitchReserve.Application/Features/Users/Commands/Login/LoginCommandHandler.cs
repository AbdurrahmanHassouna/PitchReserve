using MediatR;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Interfaces;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Common;

namespace PitchReserve.Application.Features.Users.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateJwtToken(
            user.Id.ToString(),
            user.Email,
            [user.Role.ToString()]);

        var response = new AuthResponseDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            token,
            user.OwnerProfile?.Id);

        return Result<AuthResponseDto>.Success(response);
    }
}
