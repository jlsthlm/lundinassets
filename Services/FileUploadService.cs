using Microsoft.AspNetCore.Components.Forms;

namespace LundinAssets.Services;

public class FileUploadService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public FileUploadService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> SaveAsync(IBrowserFile file, string subfolder)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return null;

        var fileName = $"{Guid.NewGuid()}{ext}";
        var folderPath = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.OpenReadStream(MaxFileSizeBytes).CopyToAsync(stream);

        return $"/uploads/{subfolder}/{fileName}";
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
