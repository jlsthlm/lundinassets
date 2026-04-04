using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LundinAssets.Models;

public class UnavailabilityPeriod
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    [Column(TypeName = "date")]
    public DateOnly StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateOnly EndDate { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
