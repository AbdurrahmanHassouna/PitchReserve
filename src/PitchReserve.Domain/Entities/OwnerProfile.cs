using PitchReserve.Domain.Common;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class OwnerProfile : BaseEntity
{
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = null!;
    public string? TaxNumber { get; private set; }
    public bool OnlinePaymentEnabled { get; private set; } = false;
    public string? StripeConnectedAccountId { get; private set; }
    public string? MaskedIBAN { get; private set; }

    public User User { get; private set; } = null!;
    public ICollection<Playground> Playgrounds { get; private set; } = new List<Playground>();

    private OwnerProfile() { }

    public static OwnerProfile Create(
        Guid userId,
        string businessName,
        string? taxNumber = null,
        string? stripeConnectedAccountId = null,
        string? maskedIBAN = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        if (string.IsNullOrWhiteSpace(businessName))
            throw new DomainException("Business name is required.");

        return new OwnerProfile
        {
            UserId = userId,
            BusinessName = businessName.Trim(),
            TaxNumber = taxNumber?.Trim(),
            StripeConnectedAccountId = stripeConnectedAccountId?.Trim(),
            MaskedIBAN = maskedIBAN?.Trim()
        };
    }

    public void UpdateBusinessDetails(string businessName, string? taxNumber, string? maskedIBAN)
    {
        if (string.IsNullOrWhiteSpace(businessName))
            throw new DomainException("Business name is required.");

        BusinessName = businessName.Trim();
        TaxNumber = taxNumber?.Trim();
        MaskedIBAN = maskedIBAN?.Trim();
    }

}
