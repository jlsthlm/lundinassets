using System.ComponentModel.DataAnnotations;

namespace LundinAssets.Models;

public class AssetPhoto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    [Required]
    public string FilePath { get; set; } = "";

    [MaxLength(200)]
    public string? Caption { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsMain { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
