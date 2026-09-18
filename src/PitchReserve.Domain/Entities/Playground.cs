using PitchReserve.Domain.Common;
using PitchReserve.Domain.Enums;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Domain.Entities;

public class Playground : BaseEntity
{
    public Guid OwnerProfileId { get; private set; }
    public string Name { get; private set; } = null!;
    public PitchSize Size { get; private set; }
    public SurfaceType SurfaceType { get; private set; }
    public decimal HourlyRate { get; private set; }
    public string LocationCity { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public TimeOnly OpenHour { get; private set; }
    public TimeOnly CloseHour { get; private set; }
    public decimal AverageRating { get; private set; } = 0;
    public int TotalRatings { get; private set; } = 0;
    public PlaygroundStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public OwnerProfile OwnerProfile { get; private set; } = null!;
    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    private Playground() { }

    public static Playground Create(
        Guid ownerProfileId,
        string name,
        PitchSize size,
        SurfaceType surfaceType,
        decimal hourlyRate,
        string locationCity,
        string address,
        TimeOnly openHour,
        TimeOnly closeHour,
        PlaygroundStatus status,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        if (ownerProfileId == Guid.Empty)
            throw new DomainException("OwnerId is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Playground name is required.");

        if (hourlyRate <= 0)
            throw new DomainException("Hourly rate must be greater than zero.");

        if (string.IsNullOrWhiteSpace(locationCity))
            throw new DomainException("Location city is required.");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Address is required.");

        ValidateHours(openHour, closeHour);
        if (status is not (PlaygroundStatus.Closed or PlaygroundStatus.Opened))
            throw new DomainException("A new playground must be opened or closed.");

        return new Playground
        {
            OwnerProfileId = ownerProfileId,
            Name = name.Trim(),
            Size = size,
            SurfaceType = surfaceType,
            HourlyRate = hourlyRate,
            LocationCity = locationCity.Trim(),
            Address = address.Trim(),
            OpenHour = openHour,
            CloseHour = closeHour,
            Latitude = latitude,
            Longitude = longitude,
            Status = status,
            AverageRating = 0,
            TotalRatings = 0
        };
    }

    public void UpdateDetails(
        string name,
        PitchSize size,
        SurfaceType surfaceType,
        decimal hourlyRate,
        string locationCity,
        string address,
        TimeOnly openHour,
        TimeOnly closeHour,
        PlaygroundStatus status,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Playground name is required.");

        if (hourlyRate <= 0)
            throw new DomainException("Hourly rate must be greater than zero.");

        if (string.IsNullOrWhiteSpace(locationCity))
            throw new DomainException("Location city is required.");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Address is required.");

        ValidateHours(openHour, closeHour);

        Name = name.Trim();
        Size = size;
        SurfaceType = surfaceType;
        HourlyRate = hourlyRate;
        LocationCity = locationCity.Trim();
        Address = address.Trim();
        OpenHour = openHour;
        CloseHour = closeHour;
        Status = status;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetRatingAggregate(int totalRatings, decimal averageRating)
    {
        if (totalRatings < 0)
            throw new DomainException("Total ratings cannot be negative.");
        if (totalRatings == 0)
        {
            TotalRatings = 0;
            AverageRating = 0;
            return;
        }
        if (averageRating is < 1 or > 5)
            throw new DomainException("Average rating must be between 1 and 5.");

        TotalRatings = totalRatings;
        AverageRating = Math.Round(averageRating, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateHours(TimeOnly openHour, TimeOnly closeHour)
    {
        if (openHour == closeHour)
            throw new DomainException("Open and close hours must be different.");
    }

}
