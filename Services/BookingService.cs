using Microsoft.EntityFrameworkCore;
using LundinAssets.Data;
using LundinAssets.Models;

namespace LundinAssets.Services;

public class BookingService
{
    private readonly ApplicationDbContext _db;

    public BookingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Booking>> GetAllAsync(int? assetId = null)
    {
        var query = _db.Bookings
            .Include(b => b.Asset)
            .Include(b => b.User)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .AsQueryable();

        if (assetId.HasValue)
            query = query.Where(b => b.AssetId == assetId.Value);

        return await query.OrderBy(b => b.StartDate).ToListAsync();
    }

    public async Task<List<Booking>> GetAllIncludingCancelledAsync()
    {
        return await _db.Bookings
            .Include(b => b.Asset)
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByUserAsync(string userId)
    {
        return await _db.Bookings
            .Include(b => b.Asset).ThenInclude(a => a.Photos)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetForRangeAsync(DateOnly from, DateOnly to, int? assetId = null)
    {
        var query = _db.Bookings
            .Include(b => b.Asset)
            .Include(b => b.User)
            .Where(b => b.Status != BookingStatus.Cancelled
                && b.StartDate <= to
                && b.EndDate >= from)
            .AsQueryable();

        if (assetId.HasValue)
            query = query.Where(b => b.AssetId == assetId.Value);

        return await query.ToListAsync();
    }

    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _db.Bookings
            .Include(b => b.Asset)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<(bool Available, string? Reason)> CheckAvailabilityAsync(
        int assetId, DateOnly start, DateOnly end, int? excludeBookingId = null)
    {
        var asset = await _db.Assets
            .Include(a => a.UnavailabilityPeriods)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset == null) return (false, "Asset not found");
        if (!asset.IsAvailable) return (false, "This asset is not available for booking");

        // Check unavailability periods
        var blocked = asset.UnavailabilityPeriods.Any(u =>
            u.StartDate <= end && u.EndDate >= start);
        if (blocked) return (false, "Selected dates overlap with a blocked period");

        // Check existing bookings
        var conflictQuery = _db.Bookings
            .Where(b => b.AssetId == assetId
                && b.Status != BookingStatus.Cancelled
                && b.StartDate <= end
                && b.EndDate >= start);

        if (excludeBookingId.HasValue)
            conflictQuery = conflictQuery.Where(b => b.Id != excludeBookingId.Value);

        var conflict = await conflictQuery.Include(b => b.User).FirstOrDefaultAsync();
        if (conflict != null)
            return (false, $"Dates overlap with an existing booking by {conflict.User.FullName}");

        return (true, null);
    }

    public async Task<Booking> CreateAsync(Booking booking)
    {
        booking.CreatedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    public async Task CancelAsync(int bookingId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking != null)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(int bookingId, BookingStatus status)
    {
        var booking = await _db.Bookings.FindAsync(bookingId);
        if (booking != null)
        {
            booking.Status = status;
            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
