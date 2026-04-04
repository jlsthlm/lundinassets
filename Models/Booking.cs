using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LundinAssets.Models;

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}

public class Booking
{
    public int Id { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateOnly EndDate { get; set; }

    public string? Notes { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int NightCount => EndDate.DayNumber - StartDate.DayNumber;
}
