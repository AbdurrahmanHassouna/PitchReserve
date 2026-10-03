namespace PitchReserve.Application.Features.Users.Queries.GetCurrentUser;

public record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber,
    Guid? OwnerProfileId,
    string? BusinessName);
