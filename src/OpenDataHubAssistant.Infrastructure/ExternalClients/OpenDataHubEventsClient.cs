using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Infrastructure.Configuration;

namespace OpenDataHubAssistant.Infrastructure.ExternalClients;

/// <summary>
/// Client for fetching events from Open Data Hub Tourism API
/// </summary>
public class OpenDataHubEventsClient : IOpenDataHubEventsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly OpenDataHubSettings _settings;
    private readonly ILogger<OpenDataHubEventsClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenDataHubEventsClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<OpenDataHubSettings> settings,
        ILogger<OpenDataHubEventsClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<IEnumerable<EventDto>> GetEventsAsync(
        double latitude, double longitude, DateTime startDate, DateTime endDate, int maxResults = 10)
    {
        var cacheKey = $"events_{latitude:F2}_{longitude:F2}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<EventDto>? cachedEvents))
        {
            _logger.LogDebug("Events cache hit for {CacheKey}", cacheKey);
            return cachedEvents ?? Enumerable.Empty<EventDto>();
        }

        try
        {
            // Open Data Hub Events API
            var url = $"{_settings.EventsApiBaseUrl}?" +
                     $"begindate={startDate:yyyy-MM-dd}&" +
                     $"enddate={endDate:yyyy-MM-dd}&" +
                     $"latitude={latitude}&" +
                     $"longitude={longitude}&" +
                     $"radius=30000&" +
                     $"pagesize={maxResults}&" +
                     $"active=true&" +
                     $"language=en";

            _logger.LogInformation("Fetching events from Open Data Hub: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var events = ParseEventsResponse(jsonString);

            _cache.Set(cacheKey, events, TimeSpan.FromMinutes(_settings.CacheTtlMinutes));

            return events.Take(maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events from Open Data Hub");
            return GetFallbackEvents(startDate, endDate, maxResults);
        }
    }

    public async Task<IEnumerable<EventDto>> GetEventsByLocationNameAsync(
        string locationName, DateTime startDate, DateTime endDate, int maxResults = 10)
    {
        var coords = GetCoordinatesForLocation(locationName);
        return await GetEventsAsync(coords.lat, coords.lon, startDate, endDate, maxResults);
    }

    private IEnumerable<EventDto> ParseEventsResponse(string jsonString)
    {
        var events = new List<EventDto>();

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Handle both direct array and paginated response
            JsonElement items;
            if (root.TryGetProperty("Items", out items) || 
                root.TryGetProperty("items", out items))
            {
                // Paginated response
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                items = root;
            }
            else
            {
                return events;
            }

            foreach (var item in items.EnumerateArray())
            {
                var evt = ParseSingleEvent(item);
                if (evt != null)
                {
                    events.Add(evt);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing events response");
        }

        return events;
    }

    private EventDto? ParseSingleEvent(JsonElement item)
    {
        try
        {
            var id = item.TryGetProperty("Id", out var idProp) ? idProp.GetString() : Guid.NewGuid().ToString();
            
            // Get detail property which contains localized content
            string name = "Unknown Event";
            string description = "";
            
            if (item.TryGetProperty("Detail", out var detail))
            {
                // Try English first, then German, then Italian
                foreach (var lang in new[] { "en", "de", "it" })
                {
                    if (detail.TryGetProperty(lang, out var langDetail))
                    {
                        if (langDetail.TryGetProperty("Title", out var title))
                            name = title.GetString() ?? name;
                        if (langDetail.TryGetProperty("BaseText", out var desc))
                            description = desc.GetString() ?? "";
                        break;
                    }
                }
            }

            // Get dates
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today;
            
            if (item.TryGetProperty("DateBegin", out var dateBegin))
                DateTime.TryParse(dateBegin.GetString(), out startDate);
            if (item.TryGetProperty("DateEnd", out var dateEnd))
                DateTime.TryParse(dateEnd.GetString(), out endDate);

            // Get location
            string location = "";
            if (item.TryGetProperty("ContactInfos", out var contacts) && 
                contacts.TryGetProperty("en", out var enContact))
            {
                if (enContact.TryGetProperty("City", out var city))
                    location = city.GetString() ?? "";
            }

            // Determine if indoor (heuristic based on event type or description)
            bool isIndoor = DetermineIfIndoor(item, description);

            return new EventDto
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name,
                Description = TruncateText(description, 500),
                StartDate = startDate,
                EndDate = endDate,
                Location = location,
                IsIndoor = isIndoor
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing single event");
            return null;
        }
    }

    private static bool DetermineIfIndoor(JsonElement item, string description)
    {
        var indoorKeywords = new[] { "museum", "theater", "theatre", "cinema", "gallery", "exhibition", 
                                      "concert", "indoor", "hall", "centro", "center" };
        var outdoorKeywords = new[] { "hiking", "ski", "outdoor", "mountain", "trail", "bike", 
                                       "garden", "park", "festival", "market" };

        var textToCheck = description.ToLower();
        
        if (item.TryGetProperty("Topics", out var topics))
        {
            textToCheck += " " + topics.ToString().ToLower();
        }

        int indoorScore = indoorKeywords.Count(k => textToCheck.Contains(k));
        int outdoorScore = outdoorKeywords.Count(k => textToCheck.Contains(k));

        return indoorScore > outdoorScore;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }

    private IEnumerable<EventDto> GetFallbackEvents(DateTime startDate, DateTime endDate, int maxResults)
    {
        // Return some sample events when API is unavailable
        return new List<EventDto>
        {
            new EventDto
            {
                Id = "fallback-1",
                Name = "Mercatino di Natale Bolzano",
                Description = "Traditional Christmas market in Bolzano's main square",
                StartDate = startDate,
                EndDate = endDate,
                Location = "Bolzano",
                IsIndoor = false
            },
            new EventDto
            {
                Id = "fallback-2",
                Name = "Museion - Museum of Modern Art",
                Description = "Contemporary art exhibitions",
                StartDate = startDate,
                EndDate = endDate,
                Location = "Bolzano",
                IsIndoor = true
            }
        }.Take(maxResults);
    }

    private static (double lat, double lon) GetCoordinatesForLocation(string locationName)
    {
        return locationName.ToLower() switch
        {
            "bolzano" or "bozen" => (46.4983, 11.3548),
            "merano" or "meran" => (46.6713, 11.1594),
            "bressanone" or "brixen" => (46.7176, 11.6565),
            "brunico" or "bruneck" => (46.7964, 11.9365),
            "vipiteno" or "sterzing" => (46.8958, 11.4328),
            _ => (46.4983, 11.3548) // Default to Bolzano
        };
    }
}
