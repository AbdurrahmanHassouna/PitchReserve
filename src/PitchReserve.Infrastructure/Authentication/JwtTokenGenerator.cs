using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly byte[] _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryHours;
    public JwtTokenGenerator(IOptions<JWTSettings> jwtSettings)
    {
        _secret = Encoding.UTF8.GetBytes(jwtSettings.Value.Secret);
        _issuer = jwtSettings.Value.Issuer;
        _audience = jwtSettings.Value.Audience;
        _expiryHours = jwtSettings.Value.ExpiryHours;
    }

    public string GenerateJwtToken(string userId, string email, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Iss, _issuer),
            new(JwtRegisteredClaimNames.Aud, _audience)
        };
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }


        var securityKey = new SymmetricSecurityKey(_secret);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_expiryHours),
            SigningCredentials = credentials,
            Issuer = _issuer,
            Audience = _audience
        };


        var jwtHandler = new JwtSecurityTokenHandler();
        SecurityToken token = jwtHandler.CreateToken(tokenDescriptor);
        return jwtHandler.WriteToken(token);
    }

}