using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LundinAssets.Models;

namespace LundinAssets.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Retry connecting to the database — MySQL container may not be ready immediately
        var retries = 10;
        while (true)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception ex) when (retries-- > 0)
            {
                logger.LogWarning("Database not ready, retrying in 3s... ({Retries} attempts left). {Message}",
                    retries, ex.Message);
                await Task.Delay(3000);
            }
        }

        // Schema migrations — add new columns to existing tables if they don't exist
        // (EnsureCreated only creates missing tables; it won't add new columns to existing ones)
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `Bookings` ADD COLUMN IF NOT EXISTS `IsFullBooking` tinyint(1) NOT NULL DEFAULT 1");
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `Bookings` ADD COLUMN IF NOT EXISTS `BedsBooked` int NULL");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Schema migration skipped or failed: {Message}", ex.Message);
        }

        // Ensure upload directories exist
        Directory.CreateDirectory(Path.Combine(env.WebRootPath, "uploads", "assets"));
        Directory.CreateDirectory(Path.Combine(env.WebRootPath, "uploads", "profiles"));

        // Seed roles
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed admin user
        var adminEmail = config["AdminUser:Email"] ?? "admin@lundinassets.se";
        var adminPassword = config["AdminUser:Password"] ?? "Admin1234!";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Lundin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed default assets if none exist
        if (!await db.Assets.AnyAsync())
        {
            var assets = new List<Asset>
            {
                new Asset
                {
                    Name = "Fjällbacka House",
                    Type = AssetType.Housing,
                    Description = "Our beautiful family house in Fjällbacka, right by the water.",
                    Location = "Fjällbacka, Sweden",
                    BedCount = 5,
                    Instructions = "Key is in the lockbox by the front door. Code is 1234.\n\nPlease take off shoes at the entrance. Check out by 11:00.",
                    IsAvailable = true,
                    Color = "#3b82f6",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Asset
                {
                    Name = "Cannes Apartment",
                    Type = AssetType.Housing,
                    Description = "Lovely apartment in Cannes with balcony view.",
                    Location = "Cannes, France",
                    BedCount = 2,
                    Instructions = "Contact the concierge upon arrival. Parking is in the underground garage.",
                    IsAvailable = true,
                    Color = "#10b981",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
            db.Assets.AddRange(assets);
            await db.SaveChangesAsync();
        }
    }
}
