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

    /// <summary>
    /// Check if an asset can be booked for the given dates.
    /// For full bookings: no other booking (full or partial) may overlap.
    /// For partial bookings: no full booking may overlap, and the total beds
    /// booked across overlapping partial bookings must not exceed the asset's BedCount.
    /// </summary>
    public async Task<(bool Available, string? Reason)> CheckAvailabilityAsync(
        int assetId, DateOnly start, DateOnly end,
        bool isFullBooking = true, int bedsRequested = 0,
        int? excludeBookingId = null)
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

        // Load all overlapping active bookings
        var overlapQuery = _db.Bookings
            .Where(b => b.AssetId == assetId
                && b.Status != BookingStatus.Cancelled
                && b.StartDate < end
                && b.EndDate > start);

        if (excludeBookingId.HasValue)
            overlapQuery = overlapQuery.Where(b => b.Id != excludeBookingId.Value);

        var overlapping = await overlapQuery.Include(b => b.User).ToListAsync();

        if (isFullBooking)
        {
            // Full booking: no overlaps allowed at all
            var conflict = overlapping.FirstOrDefault();
            if (conflict != null)
                return (false, $"Dates overlap with an existing booking by {conflict.User.FullName}");
        }
        else
        {
            // Partial booking: block if any full booking overlaps
            var fullConflict = overlapping.FirstOrDefault(b => b.IsFullBooking);
            if (fullConflict != null)
                return (false, $"{fullConflict.User.FullName} has an exclusive booking for those dates");

            // Check bed capacity day by day
            if (asset.BedCount.HasValue && bedsRequested > 0)
            {
                var partials = overlapping.Where(b => !b.IsFullBooking).ToList();
                for (var d = start; d < end; d = d.AddDays(1))
                {
                    var bedsOnDay = partials
                        .Where(b => b.StartDate <= d && b.EndDate > d)
                        .Sum(b => b.BedsBooked ?? 0);
                    var remaining = asset.BedCount.Value - bedsOnDay;
                    if (bedsRequested > remaining)
                        return (false, $"Only {remaining} bed{(remaining == 1 ? "" : "s")} available on {d:MMM d}");
                }
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Returns the minimum number of beds available across all days in the range,
    /// taking into account existing partial bookings. Returns null if the asset has
    /// no bed count or has a full booking blocking the period.
    /// </summary>
    public async Task<int?> GetAvailableBedsAsync(
        int assetId, DateOnly start, DateOnly end, int? excludeBookingId = null)
    {
        var asset = await _db.Assets.FindAsync(assetId);
        if (asset?.BedCount == null) return null;

        var overlapQuery = _db.Bookings
            .Where(b => b.AssetId == assetId
                && b.Status != BookingStatus.Cancelled
                && b.StartDate < end
                && b.EndDate > start
                && !b.IsFullBooking);

        if (excludeBookingId.HasValue)
            overlapQuery = overlapQuery.Where(b => b.Id != excludeBookingId.Value);

        var partials = await overlapQuery.ToListAsync();

        var minAvailable = asset.BedCount.Value;
        for (var d = start; d < end; d = d.AddDays(1))
        {
            var bedsOnDay = partials
                .Where(b => b.StartDate <= d && b.EndDate > d)
                .Sum(b => b.BedsBooked ?? 0);
            minAvailable = Math.Min(minAvailable, asset.BedCount.Value - bedsOnDay);
        }
        return Math.Max(0, minAvailable);
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
