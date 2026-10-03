using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;
