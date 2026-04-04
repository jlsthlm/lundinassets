using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LundinAssets.Models;

namespace LundinAssets.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Asset> Assets { get; set; }
    public DbSet<AssetPhoto> AssetPhotos { get; set; }
    public DbSet<UnavailabilityPeriod> UnavailabilityPeriods { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Booking>(b =>
        {
            b.HasOne(x => x.Asset)
                .WithMany(a => a.Bookings)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Property(x => x.StartDate).HasColumnType("date");
            b.Property(x => x.EndDate).HasColumnType("date");
        });

        builder.Entity<UnavailabilityPeriod>(u =>
        {
            u.HasOne(x => x.Asset)
                .WithMany(a => a.UnavailabilityPeriods)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Cascade);

            u.Property(x => x.StartDate).HasColumnType("date");
            u.Property(x => x.EndDate).HasColumnType("date");
        });

        builder.Entity<AssetPhoto>()
            .HasOne(x => x.Asset)
            .WithMany(a => a.Photos)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
