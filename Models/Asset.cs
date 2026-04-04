using System.ComponentModel.DataAnnotations;

namespace LundinAssets.Models;

public enum AssetType
{
    Housing = 0,
    Car = 1,
    Other = 2
}

public class Asset
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    public AssetType Type { get; set; } = AssetType.Housing;

    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    public int? BedCount { get; set; }

    public string? Instructions { get; set; }

    public bool IsAvailable { get; set; } = true;

    [MaxLength(20)]
    public string Color { get; set; } = "#3b82f6";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AssetPhoto> Photos { get; set; } = new List<AssetPhoto>();
    public ICollection<UnavailabilityPeriod> UnavailabilityPeriods { get; set; } = new List<UnavailabilityPeriod>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public AssetPhoto? PrimaryPhoto => Photos.FirstOrDefault(p => p.IsMain) ?? Photos.OrderBy(p => p.SortOrder).FirstOrDefault();

    public string TypeIcon => Type switch
    {
        AssetType.Housing => "bi-house-door",
        AssetType.Car => "bi-car-front",
        _ => "bi-box"
    };

    public string TypeLabel => Type switch
    {
        AssetType.Housing => "Housing",
        AssetType.Car => "Car",
        _ => "Other"
    };
}
