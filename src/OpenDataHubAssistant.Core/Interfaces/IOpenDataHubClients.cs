using OpenDataHubAssistant.Core.DTOs;

namespace OpenDataHubAssistant.Core.Interfaces;

/// <summary>
/// Client for fetching weather data from Open Data Hub
/// </summary>
public interface IOpenDataHubWeatherClient
{
    /// <summary>
    /// Get current weather for coordinates
    /// </summary>
    Task<WeatherDto?> GetCurrentWeatherAsync(double latitude, double longitude);

    /// <summary>
    /// Get weather forecast for a specific date
    /// </summary>
    Task<WeatherDto?> GetWeatherForDateAsync(double latitude, double longitude, DateTime date);

    /// <summary>
    /// Get weather by city name
    /// </summary>
    Task<WeatherDto?> GetWeatherByCityAsync(string cityName, DateTime? date = null);
}

/// <summary>
/// Client for fetching events from Open Data Hub
/// </summary>
public interface IOpenDataHubEventsClient
{
    /// <summary>
    /// Get events near a location for a date range
    /// </summary>
    Task<IEnumerable<EventDto>> GetEventsAsync(double latitude, double longitude, DateTime startDate, DateTime endDate, int maxResults = 10);

    /// <summary>
    /// Get events by location name
    /// </summary>
    Task<IEnumerable<EventDto>> GetEventsByLocationNameAsync(string locationName, DateTime startDate, DateTime endDate, int maxResults = 10);
}

/// <summary>
/// Client for fetching Points of Interest from Open Data Hub
/// </summary>
public interface IOpenDataHubPoiClient
{
    /// <summary>
    /// Get POIs near coordinates
    /// </summary>
    Task<IEnumerable<PoiDto>> GetPoisAsync(double latitude, double longitude, int radiusMeters = 10000, int maxResults = 10);

    /// <summary>
    /// Get POIs by location name
    /// </summary>
    Task<IEnumerable<PoiDto>> GetPoisByLocationNameAsync(string locationName, int maxResults = 10);
}
