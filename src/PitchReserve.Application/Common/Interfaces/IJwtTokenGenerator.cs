namespace PitchReserve.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateJwtToken(string userId, string email, IEnumerable<string> roles);
}
