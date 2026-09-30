using System.Security.Cryptography;
using PitchReserve.Application.Common.Interfaces;

namespace PitchReserve.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithm,
                KeySize);

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
