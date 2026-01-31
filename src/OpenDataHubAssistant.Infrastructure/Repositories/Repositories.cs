using Microsoft.EntityFrameworkCore;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Core.Models;
using OpenDataHubAssistant.Infrastructure.Data;

namespace OpenDataHubAssistant.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation
/// </summary>
public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected RepositoryBase(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Location repository implementation
/// </summary>
public class LocationRepository : RepositoryBase<Location>, ILocationRepository
{
    public LocationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Location?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(l => 
            l.Name.ToLower() == name.ToLower());
    }

    public async Task<Location?> GetByCoordinatesAsync(double latitude, double longitude)
    {
        const double tolerance = 0.01; // ~1km tolerance
        return await _dbSet.FirstOrDefaultAsync(l =>
            Math.Abs(l.Latitude - latitude) < tolerance &&
            Math.Abs(l.Longitude - longitude) < tolerance);
    }
}

/// <summary>
/// Weather snapshot repository implementation
/// </summary>
public class WeatherSnapshotRepository : RepositoryBase<WeatherSnapshot>, IWeatherSnapshotRepository
{
    public WeatherSnapshotRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<WeatherSnapshot?> GetLatestForLocationAsync(int locationId)
    {
        return await _dbSet
            .Where(w => w.LocationId == locationId)
            .OrderByDescending(w => w.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<WeatherSnapshot>> GetForLocationAndDateRangeAsync(
        int locationId, DateTime start, DateTime end)
    {
        return await _dbSet
            .Where(w => w.LocationId == locationId && 
                        w.Timestamp >= start && 
                        w.Timestamp <= end)
            .OrderBy(w => w.Timestamp)
            .ToListAsync();
    }
}

/// <summary>
/// Recommendation log repository implementation
/// </summary>
public class RecommendationLogRepository : RepositoryBase<RecommendationLog>, IRecommendationLogRepository
{
    public RecommendationLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RecommendationLog>> GetRecentForLocationAsync(int locationId, int count = 10)
    {
        return await _dbSet
            .Where(r => r.LocationId == locationId)
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
