namespace PitchReserve.Application.Features.Users.Common;

public record AuthResponseDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Token,
    Guid? OwnerProfileId = null);
