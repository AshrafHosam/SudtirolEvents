using Microsoft.AspNetCore.Mvc;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Core.Models;

namespace OpenDataHubAssistant.Api.Controllers;

/// <summary>
/// Controller for activity recommendations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IOpenDataHubWeatherClient _weatherClient;
    private readonly IOpenDataHubEventsClient _eventsClient;
    private readonly IOpenDataHubPoiClient _poiClient;
    private readonly IWeatherInterpreterService _weatherInterpreter;
    private readonly IActivityRecommendationService _recommendationService;
    private readonly ILlmService _llmService;
    private readonly ILocationRepository _locationRepository;
    private readonly IRecommendationLogRepository _logRepository;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IOpenDataHubWeatherClient weatherClient,
        IOpenDataHubEventsClient eventsClient,
        IOpenDataHubPoiClient poiClient,
        IWeatherInterpreterService weatherInterpreter,
        IActivityRecommendationService recommendationService,
        ILlmService llmService,
        ILocationRepository locationRepository,
        IRecommendationLogRepository logRepository,
        ILogger<RecommendationsController> logger)
    {
        _weatherClient = weatherClient;
        _eventsClient = eventsClient;
        _poiClient = poiClient;
        _weatherInterpreter = weatherInterpreter;
        _recommendationService = recommendationService;
        _llmService = llmService;
        _locationRepository = locationRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get AI-generated recommendations for a location
    /// </summary>
    /// <param name="locationId">Location ID from the database</param>
    /// <param name="date">Optional date (defaults to today)</param>
    [HttpGet]
    [ProducesResponseType(typeof(RecommendationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RecommendationResponseDto>> GetRecommendations(
        [FromQuery] int locationId,
        [FromQuery] DateTime? date = null)
    {
        _logger.LogInformation("Getting recommendations for locationId={LocationId}, date={Date}", locationId, date);

        try
        {
            // Get location from database
            var location = await _locationRepository.GetByIdAsync(locationId);
            if (location == null)
            {
                return NotFound(new { message = $"Location with ID {locationId} not found" });
            }

            var targetDate = date ?? DateTime.Today;

            // Fetch all data from Open Data Hub APIs
            var weather = await _weatherClient.GetWeatherForDateAsync(
                location.Latitude, location.Longitude, targetDate);

            if (weather == null)
            {
                return Problem(
                    detail: "Unable to fetch weather data",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            weather.LocationName = location.Name;

            var classifications = _weatherInterpreter.ClassifyWeather(weather);
            var events = (await _eventsClient.GetEventsAsync(
                location.Latitude, location.Longitude, targetDate, targetDate.AddDays(1), 10)).ToList();
            var pois = (await _poiClient.GetPoisAsync(
                location.Latitude, location.Longitude, 15000, 10)).ToList();

            // Generate recommendations
            var recommendations = await _recommendationService.GetRecommendationsAsync(
                weather, classifications, events, pois, 5);

            // Generate AI explanation
            var explanation = await _llmService.GenerateRecommendationExplanationAsync(
                weather, classifications, recommendations);

            // Log the recommendation
            await _logRepository.AddAsync(new RecommendationLog
            {
                LocationId = locationId,
                Timestamp = DateTime.UtcNow,
                QueryText = $"Recommendations for {location.Name} on {targetDate:yyyy-MM-dd}",
                RecommendationText = explanation,
                SourceDataSummary = $"Weather: {weather.ConditionText}, Events: {events.Count}, POIs: {pois.Count}"
            });

            var response = new RecommendationResponseDto
            {
                Explanation = explanation,
                Weather = weather,
                Classifications = classifications,
                Recommendations = recommendations,
                SourceEvents = events.Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Location = e.Location,
                    IsIndoor = e.IsIndoor
                }).ToList(),
                SourcePois = pois.Select(p => new PoiDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Type = p.Type,
                    Description = p.Description,
                    Address = p.Address,
                    IsIndoor = p.IsIndoor,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for location {LocationId}", locationId);
            return Problem(
                detail: "An error occurred while generating recommendations",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get AI-generated recommendations by city name
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="date">Optional date (defaults to today)</param>
    [HttpGet("city/{city}")]
    [ProducesResponseType(typeof(RecommendationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RecommendationResponseDto>> GetRecommendationsByCity(
        string city,
        [FromQuery] DateTime? date = null)
    {
        _logger.LogInformation("Getting recommendations for city={City}, date={Date}", city, date);

        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { message = "City name is required" });
        }

        try
        {
            var targetDate = date ?? DateTime.Today;

            // Fetch all data from Open Data Hub APIs
            var weather = await _weatherClient.GetWeatherByCityAsync(city, targetDate);

            if (weather == null)
            {
                return Problem(
                    detail: $"Unable to fetch weather data for {city}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var classifications = _weatherInterpreter.ClassifyWeather(weather);
            var events = (await _eventsClient.GetEventsByLocationNameAsync(
                city, targetDate, targetDate.AddDays(1), 10)).ToList();
            var pois = (await _poiClient.GetPoisByLocationNameAsync(city, 10)).ToList();

            // Generate recommendations
            var recommendations = await _recommendationService.GetRecommendationsAsync(
                weather, classifications, events, pois, 5);

            // Generate AI explanation
            var explanation = await _llmService.GenerateRecommendationExplanationAsync(
                weather, classifications, recommendations);

            // Try to log the recommendation
            var location = await _locationRepository.GetByNameAsync(city);
            if (location != null)
            {
                await _logRepository.AddAsync(new RecommendationLog
                {
                    LocationId = location.Id,
                    Timestamp = DateTime.UtcNow,
                    QueryText = $"Recommendations for {city} on {targetDate:yyyy-MM-dd}",
                    RecommendationText = explanation,
                    SourceDataSummary = $"Weather: {weather.ConditionText}, Events: {events.Count}, POIs: {pois.Count}"
                });
            }

            var response = new RecommendationResponseDto
            {
                Explanation = explanation,
                Weather = weather,
                Classifications = classifications,
                Recommendations = recommendations,
                SourceEvents = events,
                SourcePois = pois
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for city {City}", city);
            return Problem(
                detail: "An error occurred while generating recommendations",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
