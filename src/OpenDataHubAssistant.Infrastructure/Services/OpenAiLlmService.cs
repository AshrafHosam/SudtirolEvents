using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Enums;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Infrastructure.Configuration;

namespace OpenDataHubAssistant.Infrastructure.Services;

/// <summary>
/// OpenAI-based LLM service for generating natural language recommendations
/// </summary>
public class OpenAiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;
    private readonly IOpenDataHubWeatherClient _weatherClient;
    private readonly IOpenDataHubEventsClient _eventsClient;
    private readonly IOpenDataHubPoiClient _poiClient;
    private readonly IWeatherInterpreterService _weatherInterpreter;
    private readonly IActivityRecommendationService _recommendationService;
    private readonly ILogger<OpenAiLlmService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAiLlmService(
        HttpClient httpClient,
        IOptions<OpenAiSettings> settings,
        IOpenDataHubWeatherClient weatherClient,
        IOpenDataHubEventsClient eventsClient,
        IOpenDataHubPoiClient poiClient,
        IWeatherInterpreterService weatherInterpreter,
        IActivityRecommendationService recommendationService,
        ILogger<OpenAiLlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _weatherClient = weatherClient;
        _eventsClient = eventsClient;
        _poiClient = poiClient;
        _weatherInterpreter = weatherInterpreter;
        _recommendationService = recommendationService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Configure HttpClient for OpenAI
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task<string> GenerateRecommendationExplanationAsync(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured, using fallback explanation");
            return GenerateFallbackExplanation(weather, classifications, recommendations);
        }

        try
        {
            var prompt = BuildRecommendationPrompt(weather, classifications, recommendations);
            var response = await CallOpenAiAsync(prompt);
            return response ?? GenerateFallbackExplanation(weather, classifications, recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API, using fallback");
            return GenerateFallbackExplanation(weather, classifications, recommendations);
        }
    }

    public async Task<ChatResponseDto> ProcessChatMessageAsync(string userMessage)
    {
        var response = new ChatResponseDto();

        try
        {
            // Parse the user message to extract location and date
            var (location, date) = ParseUserMessage(userMessage);

            _logger.LogInformation("Processing chat message for location: {Location}, date: {Date}", location, date);

            // Fetch weather data
            var weather = await _weatherClient.GetWeatherByCityAsync(location, date);
            if (weather == null)
            {
                response.Response = $"I couldn't fetch weather data for {location}. Please try again or specify a different location in South Tyrol.";
                return response;
            }

            // Classify weather
            var classifications = _weatherInterpreter.ClassifyWeather(weather);

            // Fetch events and POIs
            var events = await _eventsClient.GetEventsByLocationNameAsync(location, date, date.AddDays(1), 5);
            var pois = await _poiClient.GetPoisByLocationNameAsync(location, 5);

            // Generate recommendations
            var recommendations = await _recommendationService.GetRecommendationsAsync(
                weather, classifications, events, pois, 5);

            // Build the data response
            response.Data = new RecommendationResponseDto
            {
                Weather = weather,
                Classifications = classifications,
                Recommendations = recommendations,
                SourceEvents = events.ToList(),
                SourcePois = pois.ToList()
            };

            // Generate natural language response
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                response.Response = GenerateFallbackChatResponse(weather, classifications, recommendations, location, date);
                response.Data.Explanation = response.Response;
            }
            else
            {
                var chatPrompt = BuildChatPrompt(userMessage, weather, classifications, recommendations, events.ToList(), pois.ToList());
                var llmResponse = await CallOpenAiAsync(chatPrompt);
                response.Response = llmResponse ?? GenerateFallbackChatResponse(weather, classifications, recommendations, location, date);
                response.Data.Explanation = response.Response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            response.Response = "I'm sorry, I encountered an error processing your request. Please try again.";
        }

        return response;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
            return false;

        try
        {
            // Simple health check
            var testPrompt = "Say 'OK' if you can read this.";
            var response = await CallOpenAiAsync(testPrompt, maxTokens: 10);
            return !string.IsNullOrEmpty(response);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> CallOpenAiAsync(string prompt, int? maxTokens = null)
    {
        var url = $"{_settings.BaseUrl}/chat/completions";

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful travel assistant for South Tyrol, Italy. Provide concise, friendly recommendations based on weather and available activities." },
                new { role = "user", content = prompt }
            },
            max_tokens = maxTokens ?? _settings.MaxTokens,
            temperature = _settings.Temperature
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        _logger.LogDebug("Calling OpenAI API: {Url}", url);

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var messageContent))
            {
                return messageContent.GetString();
            }
        }

        return null;
    }

    private static string BuildRecommendationPrompt(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Weather in {weather.LocationName} on {weather.Timestamp:MMM dd}:");
        sb.AppendLine($"- Temperature: {weather.TemperatureC:F1}°C");
        sb.AppendLine($"- Conditions: {weather.ConditionText}");
        sb.AppendLine($"- Classification: {string.Join(", ", classifications)}");
        sb.AppendLine();
        sb.AppendLine("Recommended activities:");
        foreach (var rec in recommendations.Take(5))
        {
            sb.AppendLine($"- {rec.Name} ({(rec.IsIndoor ? "Indoor" : "Outdoor")}, {rec.Type})");
        }
        sb.AppendLine();
        sb.AppendLine("Write a brief (2-3 sentences) natural explanation of why these activities are recommended given the weather. Be friendly and helpful.");

        return sb.ToString();
    }

    private static string BuildChatPrompt(
        string userMessage,
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations,
        List<EventDto> events,
        List<PoiDto> pois)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"User asked: \"{userMessage}\"");
        sb.AppendLine();
        sb.AppendLine($"Current data for {weather.LocationName} on {weather.Timestamp:MMM dd, yyyy}:");
        sb.AppendLine($"Weather: {weather.TemperatureC:F1}°C, {weather.ConditionText}");
        sb.AppendLine($"Wind: {weather.WindKph:F0} km/h, Precipitation: {weather.PrecipitationMm:F1}mm");
        sb.AppendLine($"Classification: {string.Join(", ", classifications)}");
        sb.AppendLine();

        if (events.Any())
        {
            sb.AppendLine("Available events:");
            foreach (var evt in events.Take(3))
            {
                sb.AppendLine($"- {evt.Name} ({(evt.IsIndoor ? "Indoor" : "Outdoor")})");
            }
        }

        if (pois.Any())
        {
            sb.AppendLine("Points of interest:");
            foreach (var poi in pois.Take(3))
            {
                sb.AppendLine($"- {poi.Name} ({poi.Type}, {(poi.IsIndoor ? "Indoor" : "Outdoor")})");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Based on this information, provide a helpful, conversational response (3-5 sentences) that:");
        sb.AppendLine("1. Acknowledges the weather conditions");
        sb.AppendLine("2. Recommends 2-3 specific activities with brief reasons");
        sb.AppendLine("3. Mentions if indoor or outdoor activities are preferred");

        return sb.ToString();
    }

    private static (string location, DateTime date) ParseUserMessage(string message)
    {
        var lowerMessage = message.ToLower();

        // Extract location
        string location = "Bolzano"; // Default
        var locations = new[] { "bolzano", "bozen", "merano", "meran", "bressanone", "brixen",
                                "brunico", "bruneck", "vipiteno", "sterzing", "ortisei" };
        foreach (var loc in locations)
        {
            if (lowerMessage.Contains(loc))
            {
                location = char.ToUpper(loc[0]) + loc[1..];
                break;
            }
        }

        // Extract date
        DateTime date = DateTime.Today;
        
        if (lowerMessage.Contains("tomorrow"))
        {
            date = DateTime.Today.AddDays(1);
        }
        else if (lowerMessage.Contains("day after tomorrow"))
        {
            date = DateTime.Today.AddDays(2);
        }
        else if (lowerMessage.Contains("next week"))
        {
            date = DateTime.Today.AddDays(7);
        }
        else if (Regex.IsMatch(lowerMessage, @"in (\d+) days?"))
        {
            var match = Regex.Match(lowerMessage, @"in (\d+) days?");
            if (int.TryParse(match.Groups[1].Value, out int days))
            {
                date = DateTime.Today.AddDays(days);
            }
        }
        else if (TryParseDayOfWeek(lowerMessage, out DateTime dayOfWeekDate))
        {
            date = dayOfWeekDate;
        }
        else if (TryParseMonthDay(lowerMessage, out DateTime monthDayDate))
        {
            date = monthDayDate;
        }
        else
        {
            // Try to find a date pattern like dd/mm or dd-mm-yyyy
            var dateMatch = Regex.Match(message, @"\b(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?\b");
            if (dateMatch.Success)
            {
                var day = int.Parse(dateMatch.Groups[1].Value);
                var month = int.Parse(dateMatch.Groups[2].Value);
                var year = dateMatch.Groups[3].Success ? int.Parse(dateMatch.Groups[3].Value) : DateTime.Today.Year;
                if (year < 100) year += 2000;

                try
                {
                    date = new DateTime(year, month, day);
                }
                catch { }
            }
        }

        return (location, date);
    }

    private static bool TryParseDayOfWeek(string message, out DateTime result)
    {
        result = DateTime.Today;
        var daysOfWeek = new Dictionary<string, DayOfWeek>
        {
            { "monday", DayOfWeek.Monday },
            { "tuesday", DayOfWeek.Tuesday },
            { "wednesday", DayOfWeek.Wednesday },
            { "thursday", DayOfWeek.Thursday },
            { "friday", DayOfWeek.Friday },
            { "saturday", DayOfWeek.Saturday },
            { "sunday", DayOfWeek.Sunday }
        };

        bool isNextWeek = message.Contains("next ");
        
        foreach (var (dayName, dayOfWeek) in daysOfWeek)
        {
            if (message.Contains(dayName))
            {
                var today = DateTime.Today;
                int daysUntil = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
                
                // If it's the same day and "next" is specified, or if daysUntil is 0, go to next week
                if (daysUntil == 0 || isNextWeek)
                {
                    daysUntil += 7;
                }
                
                result = today.AddDays(daysUntil);
                return true;
            }
        }
        
        return false;
    }

    private static bool TryParseMonthDay(string message, out DateTime result)
    {
        result = DateTime.Today;
        
        var months = new Dictionary<string, int>
        {
            { "january", 1 }, { "jan", 1 },
            { "february", 2 }, { "feb", 2 },
            { "march", 3 }, { "mar", 3 },
            { "april", 4 }, { "apr", 4 },
            { "may", 5 },
            { "june", 6 }, { "jun", 6 },
            { "july", 7 }, { "jul", 7 },
            { "august", 8 }, { "aug", 8 },
            { "september", 9 }, { "sep", 9 }, { "sept", 9 },
            { "october", 10 }, { "oct", 10 },
            { "november", 11 }, { "nov", 11 },
            { "december", 12 }, { "dec", 12 }
        };

        foreach (var (monthName, monthNum) in months)
        {
            // Match patterns like "February 3", "Feb 3rd", "3rd of February", "3 February"
            var patterns = new[]
            {
                $@"{monthName}\s+(\d{{1,2}})(?:st|nd|rd|th)?",  // February 3rd
                $@"(\d{{1,2}})(?:st|nd|rd|th)?\s+(?:of\s+)?{monthName}"  // 3rd of February, 3 February
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int day))
                {
                    var year = DateTime.Today.Year;
                    try
                    {
                        result = new DateTime(year, monthNum, day);
                        // If the date is in the past, assume next year
                        if (result < DateTime.Today)
                        {
                            result = result.AddYears(1);
                        }
                        return true;
                    }
                    catch { }
                }
            }
        }
        
        return false;
    }

    private static string GenerateFallbackExplanation(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations)
    {
        var sb = new StringBuilder();

        bool isBadWeather = classifications.Contains(WeatherClassification.Bad);

        if (isBadWeather)
        {
            sb.Append($"Given the current weather conditions in {weather.LocationName} ");
            sb.Append($"({weather.TemperatureC:F0}°C, {weather.ConditionText}), ");
            sb.Append("we recommend focusing on indoor activities. ");
        }
        else
        {
            sb.Append($"The weather in {weather.LocationName} looks great ");
            sb.Append($"({weather.TemperatureC:F0}°C, {weather.ConditionText})! ");
            sb.Append("It's a perfect day to explore outdoor activities. ");
        }

        var indoorCount = recommendations.Count(r => r.IsIndoor);
        var outdoorCount = recommendations.Count(r => !r.IsIndoor);

        if (recommendations.Any())
        {
            sb.Append($"We found {recommendations.Count} recommendations for you");
            if (indoorCount > 0 && outdoorCount > 0)
            {
                sb.Append($" ({indoorCount} indoor, {outdoorCount} outdoor)");
            }
            sb.Append(".");
        }

        return sb.ToString();
    }

    private static string GenerateFallbackChatResponse(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        List<ActivityRecommendationDto> recommendations,
        string location,
        DateTime date)
    {
        var sb = new StringBuilder();

        bool isBadWeather = classifications.Contains(WeatherClassification.Bad);
        var dateStr = date.Date == DateTime.Today ? "today" :
                      date.Date == DateTime.Today.AddDays(1) ? "tomorrow" :
                      date.ToString("MMMM d");

        sb.AppendLine($"Here's what I found for {location} {dateStr}!");
        sb.AppendLine();
        sb.AppendLine($"**Weather:** {weather.TemperatureC:F0}°C, {weather.ConditionText}");

        if (isBadWeather)
        {
            sb.AppendLine();
            sb.AppendLine("The weather suggests indoor activities might be more comfortable. Here are my top picks:");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("Great weather for being outdoors! Here are my recommendations:");
        }

        sb.AppendLine();
        foreach (var rec in recommendations.Take(3))
        {
            var icon = rec.IsIndoor ? "???" : "??";
            sb.AppendLine($"{icon} **{rec.Name}** - {rec.Description}");
        }

        return sb.ToString();
    }
}
