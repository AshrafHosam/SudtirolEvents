using OpenDataHubAssistant.Core.Models;

namespace OpenDataHubAssistant.Core.Interfaces;

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

/// <summary>
/// Location repository interface
/// </summary>
public interface ILocationRepository : IRepository<Location>
{
    Task<Location?> GetByNameAsync(string name);
    Task<Location?> GetByCoordinatesAsync(double latitude, double longitude);
}

/// <summary>
/// Weather snapshot repository interface
/// </summary>
public interface IWeatherSnapshotRepository : IRepository<WeatherSnapshot>
{
    Task<WeatherSnapshot?> GetLatestForLocationAsync(int locationId);
    Task<IEnumerable<WeatherSnapshot>> GetForLocationAndDateRangeAsync(int locationId, DateTime start, DateTime end);
}

/// <summary>
/// Recommendation log repository interface
/// </summary>
public interface IRecommendationLogRepository : IRepository<RecommendationLog>
{
    Task<IEnumerable<RecommendationLog>> GetRecentForLocationAsync(int locationId, int count = 10);
}
