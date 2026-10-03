using MediatR;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Common;
using PitchReserve.Domain.Enums;

namespace PitchReserve.Application.Features.Users.Commands.RegisterUser;

public record RegisterUserCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    string? PhoneNumber = null,
    string? BusinessName = null,
    string? TaxNumber = null) : IRequest<Result<AuthResponseDto>>;
