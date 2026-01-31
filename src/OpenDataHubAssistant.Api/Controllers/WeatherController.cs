using Microsoft.AspNetCore.Mvc;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Interfaces;

namespace OpenDataHubAssistant.Api.Controllers;

/// <summary>
/// Controller for weather-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IOpenDataHubWeatherClient _weatherClient;
    private readonly IOpenDataHubEventsClient _eventsClient;
    private readonly IOpenDataHubPoiClient _poiClient;
    private readonly IWeatherInterpreterService _weatherInterpreter;
    private readonly IActivityRecommendationService _recommendationService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IOpenDataHubWeatherClient weatherClient,
        IOpenDataHubEventsClient eventsClient,
        IOpenDataHubPoiClient poiClient,
        IWeatherInterpreterService weatherInterpreter,
        IActivityRecommendationService recommendationService,
        ILogger<WeatherController> logger)
    {
        _weatherClient = weatherClient;
        _eventsClient = eventsClient;
        _poiClient = poiClient;
        _weatherInterpreter = weatherInterpreter;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Get weather data with classification and recommendations for given coordinates
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lon">Longitude</param>
    /// <param name="date">Optional date (defaults to today)</param>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherResponseDto>> GetWeather(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] DateTime? date = null)
    {
        _logger.LogInformation("Getting weather for lat={Lat}, lon={Lon}, date={Date}", lat, lon, date);

        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
        {
            return BadRequest(new { message = "Invalid coordinates. Latitude must be between -90 and 90, longitude between -180 and 180." });
        }

        try
        {
            var targetDate = date ?? DateTime.Today;

            // Fetch weather from Open Data Hub
            var weather = await _weatherClient.GetWeatherForDateAsync(lat, lon, targetDate);

            if (weather == null)
            {
                return Problem(
                    detail: "Unable to fetch weather data from Open Data Hub",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Classify weather
            var classifications = _weatherInterpreter.ClassifyWeather(weather);

            // Fetch events and POIs for recommendations
            var events = await _eventsClient.GetEventsAsync(lat, lon, targetDate, targetDate.AddDays(1), 5);
            var pois = await _poiClient.GetPoisAsync(lat, lon, 15000, 5);

            // Generate recommendations
            var recommendations = await _recommendationService.GetRecommendationsAsync(
                weather, classifications, events, pois, 5);

            var response = new WeatherResponseDto
            {
                Weather = weather,
                Classifications = classifications,
                Recommendations = recommendations
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data");
            return Problem(
                detail: "An error occurred while processing the weather request",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get weather data by city name
    /// </summary>
    /// <param name="city">City name (e.g., Bolzano, Merano)</param>
    /// <param name="date">Optional date (defaults to today)</param>
    [HttpGet("city/{city}")]
    [ProducesResponseType(typeof(WeatherResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WeatherResponseDto>> GetWeatherByCity(
        string city,
        [FromQuery] DateTime? date = null)
    {
        _logger.LogInformation("Getting weather for city={City}, date={Date}", city, date);

        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { message = "City name is required" });
        }

        try
        {
            var targetDate = date ?? DateTime.Today;

            // Fetch weather from Open Data Hub
            var weather = await _weatherClient.GetWeatherByCityAsync(city, targetDate);

            if (weather == null)
            {
                return Problem(
                    detail: $"Unable to fetch weather data for {city}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Classify weather
            var classifications = _weatherInterpreter.ClassifyWeather(weather);

            // Fetch events and POIs for recommendations
            var events = await _eventsClient.GetEventsByLocationNameAsync(city, targetDate, targetDate.AddDays(1), 5);
            var pois = await _poiClient.GetPoisByLocationNameAsync(city, 5);

            // Generate recommendations
            var recommendations = await _recommendationService.GetRecommendationsAsync(
                weather, classifications, events, pois, 5);

            var response = new WeatherResponseDto
            {
                Weather = weather,
                Classifications = classifications,
                Recommendations = recommendations
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data for city {City}", city);
            return Problem(
                detail: "An error occurred while processing the weather request",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
