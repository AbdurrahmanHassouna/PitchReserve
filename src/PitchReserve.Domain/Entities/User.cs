using PitchReserve.Domain.Common;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Player;
    public string? PasswordHash { get; private set; }
    public string? GoogleSubjectId { get; private set; }

    public OwnerProfile? OwnerProfile { get; private set; }
    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public ICollection<MatchParticipant> MatchParticipations { get; private set; } = new List<MatchParticipant>();
    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    private User() { }

    private User(string fullName, string email, UserRole role, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        PhoneNumber = phoneNumber?.Trim();
    }

    public static User CreateWithPassword(
        string fullName,
        string email,
        UserRole role,
        string? phoneNumber,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        var user = new User(fullName, email, role, phoneNumber)
        {
            PasswordHash = passwordHash
        };

        return user;
    }

    public static User CreateWithGoogle(
        string fullName,
        string email,
        UserRole role,
        string? phoneNumber,
        string googleSubjectId)
    {
        if (string.IsNullOrWhiteSpace(googleSubjectId))
            throw new DomainException("Google Subject ID is required.");

        var user = new User(fullName, email, role, phoneNumber)
        {
            GoogleSubjectId = googleSubjectId.Trim()
        };

        return user;
    }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = passwordHash;
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
    }

    public void LinkGoogleSubjectId(string googleSubjectId)
    {
        if (string.IsNullOrWhiteSpace(googleSubjectId))
            throw new DomainException("Google Subject ID is invalid.");

        GoogleSubjectId = googleSubjectId.Trim();
    }
}