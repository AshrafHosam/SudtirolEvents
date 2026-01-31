using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Enums;

namespace OpenDataHubAssistant.Core.Interfaces;

/// <summary>
/// Service for interpreting and classifying weather conditions
/// </summary>
public interface IWeatherInterpreterService
{
    /// <summary>
    /// Classify weather into one or more categories
    /// </summary>
    List<WeatherClassification> ClassifyWeather(WeatherDto weather);

    /// <summary>
    /// Determine if weather is suitable for outdoor activities
    /// </summary>
    bool IsSuitableForOutdoor(WeatherDto weather);

    /// <summary>
    /// Get a short text description of the weather classification
    /// </summary>
    string GetClassificationSummary(List<WeatherClassification> classifications);
}

/// <summary>
/// Service for generating activity recommendations
/// </summary>
public interface IActivityRecommendationService
{
    /// <summary>
    /// Generate activity recommendations based on weather, events, and POIs
    /// </summary>
    Task<List<ActivityRecommendationDto>> GetRecommendationsAsync(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        IEnumerable<EventDto> events,
        IEnumerable<PoiDto> pois,
        int maxRecommendations = 5);
}

/// <summary>
/// Service for LLM-based natural language generation
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Generate a natural language explanation for recommendations
    /// </summary>
    Task<string> GenerateRecommendationExplanationAsync(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations);

    /// <summary>
    /// Process a chat message and generate a response
    /// </summary>
    Task<ChatResponseDto> ProcessChatMessageAsync(string userMessage);

    /// <summary>
    /// Check if the LLM service is available
    /// </summary>
    Task<bool> IsAvailableAsync();
}
