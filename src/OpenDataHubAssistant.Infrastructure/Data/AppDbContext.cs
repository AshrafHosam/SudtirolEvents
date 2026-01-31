using Microsoft.EntityFrameworkCore;
using OpenDataHubAssistant.Core.Models;

namespace OpenDataHubAssistant.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PointOfInterest> PointsOfInterest => Set<PointOfInterest>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Location configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        // WeatherSnapshot configuration
        modelBuilder.Entity<WeatherSnapshot>(entity =>
        {
            entity.HasIndex(e => new { e.LocationId, e.Timestamp });
            entity.HasOne(e => e.Location)
                  .WithMany(l => l.WeatherSnapshots)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        // PointOfInterest configuration
        modelBuilder.Entity<PointOfInterest>(entity =>
        {
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.Type);
        });

        // RecommendationLog configuration
        modelBuilder.Entity<RecommendationLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasOne(e => e.Location)
                  .WithMany(l => l.RecommendationLogs)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed initial locations for South Tyrol
        modelBuilder.Entity<Location>().HasData(
            new Location { Id = 1, Name = "Bolzano", Latitude = 46.4983, Longitude = 11.3548 },
            new Location { Id = 2, Name = "Merano", Latitude = 46.6713, Longitude = 11.1594 },
            new Location { Id = 3, Name = "Bressanone", Latitude = 46.7176, Longitude = 11.6565 },
            new Location { Id = 4, Name = "Brunico", Latitude = 46.7964, Longitude = 11.9365 },
            new Location { Id = 5, Name = "Vipiteno", Latitude = 46.8958, Longitude = 11.4328 }
        );
    }
}
