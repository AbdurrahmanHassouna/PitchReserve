using MediatR;
using PitchReserve.Application.Common.Models;
using PitchReserve.Application.Features.Users.Common;

namespace PitchReserve.Application.Features.Users.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;
