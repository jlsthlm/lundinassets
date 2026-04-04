using Microsoft.EntityFrameworkCore;
using LundinAssets.Data;
using LundinAssets.Models;

namespace LundinAssets.Services;

public class AssetService
{
    private readonly ApplicationDbContext _db;

    public AssetService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Asset>> GetAllAsync()
    {
        return await _db.Assets
            .Include(a => a.Photos)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Asset?> GetByIdAsync(int id)
    {
        return await _db.Assets
            .Include(a => a.Photos)
            .Include(a => a.UnavailabilityPeriods)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Asset> CreateAsync(Asset asset)
    {
        asset.CreatedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();
        return asset;
    }

    public async Task UpdateAsync(Asset asset)
    {
        asset.UpdatedAt = DateTime.UtcNow;
        _db.Assets.Update(asset);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _db.Assets
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (asset != null)
        {
            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
        }
    }

    public async Task AddPhotoAsync(AssetPhoto photo)
    {
        _db.AssetPhotos.Add(photo);
        await _db.SaveChangesAsync();
    }

    public async Task DeletePhotoAsync(int photoId)
    {
        var photo = await _db.AssetPhotos.FindAsync(photoId);
        if (photo != null)
        {
            _db.AssetPhotos.Remove(photo);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SetMainPhotoAsync(int assetId, int photoId)
    {
        var photos = await _db.AssetPhotos.Where(p => p.AssetId == assetId).ToListAsync();
        foreach (var p in photos)
            p.IsMain = p.Id == photoId;
        await _db.SaveChangesAsync();
    }

    public async Task AddUnavailabilityAsync(UnavailabilityPeriod period)
    {
        _db.UnavailabilityPeriods.Add(period);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUnavailabilityAsync(int id)
    {
        var period = await _db.UnavailabilityPeriods.FindAsync(id);
        if (period != null)
        {
            _db.UnavailabilityPeriods.Remove(period);
            await _db.SaveChangesAsync();
        }
    }
}
