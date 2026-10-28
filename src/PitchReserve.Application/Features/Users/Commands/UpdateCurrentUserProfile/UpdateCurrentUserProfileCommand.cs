using MediatR;
using PitchReserve.Application.Common.Models;

namespace PitchReserve.Application.Features.Users.Commands.UpdateCurrentUserProfile;

public record UpdateCurrentUserProfileCommand(string? FullName, string? PhoneNumber) : IRequest<Result>;
