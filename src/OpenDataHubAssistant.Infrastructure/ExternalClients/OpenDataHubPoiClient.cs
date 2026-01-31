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
/// Client for fetching Points of Interest from Open Data Hub Tourism API
/// </summary>
public class OpenDataHubPoiClient : IOpenDataHubPoiClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly OpenDataHubSettings _settings;
    private readonly ILogger<OpenDataHubPoiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenDataHubPoiClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<OpenDataHubSettings> settings,
        ILogger<OpenDataHubPoiClient> logger)
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

    public async Task<IEnumerable<PoiDto>> GetPoisAsync(
        double latitude, double longitude, int radiusMeters = 10000, int maxResults = 10)
    {
        var cacheKey = $"pois_{latitude:F2}_{longitude:F2}_{radiusMeters}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PoiDto>? cachedPois))
        {
            _logger.LogDebug("POIs cache hit for {CacheKey}", cacheKey);
            return cachedPois ?? Enumerable.Empty<PoiDto>();
        }

        try
        {
            // Open Data Hub ODHActivityPoi API
            var url = $"{_settings.PoiApiBaseUrl}?" +
                     $"latitude={latitude}&" +
                     $"longitude={longitude}&" +
                     $"radius={radiusMeters}&" +
                     $"pagesize={maxResults}&" +
                     $"active=true&" +
                     $"language=en";

            _logger.LogInformation("Fetching POIs from Open Data Hub: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var pois = ParsePoisResponse(jsonString);

            _cache.Set(cacheKey, pois, TimeSpan.FromMinutes(_settings.CacheTtlMinutes));

            return pois.Take(maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching POIs from Open Data Hub");
            return GetFallbackPois(maxResults);
        }
    }

    public async Task<IEnumerable<PoiDto>> GetPoisByLocationNameAsync(string locationName, int maxResults = 10)
    {
        var coords = GetCoordinatesForLocation(locationName);
        return await GetPoisAsync(coords.lat, coords.lon, 15000, maxResults);
    }

    private IEnumerable<PoiDto> ParsePoisResponse(string jsonString)
    {
        var pois = new List<PoiDto>();

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
                return pois;
            }

            foreach (var item in items.EnumerateArray())
            {
                var poi = ParseSinglePoi(item);
                if (poi != null)
                {
                    pois.Add(poi);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing POIs response");
        }

        return pois;
    }

    private PoiDto? ParseSinglePoi(JsonElement item)
    {
        try
        {
            var id = item.TryGetProperty("Id", out var idProp) ? idProp.GetString() : Guid.NewGuid().ToString();

            // Get detail property which contains localized content
            string name = "Unknown POI";
            string description = "";
            string type = "Attraction";

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

            // Get type/category
            if (item.TryGetProperty("Type", out var typeProp))
                type = typeProp.GetString() ?? type;
            else if (item.TryGetProperty("SubType", out var subTypeProp))
                type = subTypeProp.GetString() ?? type;

            // Get address
            string address = "";
            if (item.TryGetProperty("ContactInfos", out var contacts))
            {
                foreach (var lang in new[] { "en", "de", "it" })
                {
                    if (contacts.TryGetProperty(lang, out var langContact))
                    {
                        var addressParts = new List<string>();
                        if (langContact.TryGetProperty("Address", out var addr))
                            addressParts.Add(addr.GetString() ?? "");
                        if (langContact.TryGetProperty("City", out var city))
                            addressParts.Add(city.GetString() ?? "");
                        address = string.Join(", ", addressParts.Where(p => !string.IsNullOrEmpty(p)));
                        break;
                    }
                }
            }

            // Get coordinates
            double? lat = null, lon = null;
            if (item.TryGetProperty("GpsInfo", out var gps) && gps.ValueKind == JsonValueKind.Array)
            {
                var firstGps = gps.EnumerateArray().FirstOrDefault();
                if (firstGps.ValueKind != JsonValueKind.Undefined)
                {
                    if (firstGps.TryGetProperty("Latitude", out var latProp))
                        lat = latProp.GetDouble();
                    if (firstGps.TryGetProperty("Longitude", out var lonProp))
                        lon = lonProp.GetDouble();
                }
            }

            // Determine if indoor
            bool isIndoor = DetermineIfIndoor(type, description);

            return new PoiDto
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Description = TruncateText(description, 500),
                Address = address,
                IsIndoor = isIndoor,
                Latitude = lat,
                Longitude = lon
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing single POI");
            return null;
        }
    }

    private static bool DetermineIfIndoor(string type, string description)
    {
        var indoorTypes = new[] { "museum", "gallery", "church", "castle", "theater", "theatre", 
                                   "cinema", "spa", "wellness", "shopping", "restaurant", "cafe" };
        var outdoorTypes = new[] { "hiking", "trail", "mountain", "lake", "park", "garden", 
                                    "ski", "bike", "climbing", "nature" };

        var textToCheck = (type + " " + description).ToLower();

        int indoorScore = indoorTypes.Count(k => textToCheck.Contains(k));
        int outdoorScore = outdoorTypes.Count(k => textToCheck.Contains(k));

        return indoorScore > outdoorScore;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }

    private IEnumerable<PoiDto> GetFallbackPois(int maxResults)
    {
        // Return some sample POIs when API is unavailable
        return new List<PoiDto>
        {
            new PoiDto
            {
                Id = "fallback-1",
                Name = "Castel Roncolo",
                Type = "Castle",
                Description = "Medieval castle with beautiful frescoes",
                Address = "Bolzano",
                IsIndoor = true,
                Latitude = 46.5167,
                Longitude = 11.3500
            },
            new PoiDto
            {
                Id = "fallback-2",
                Name = "Renon Cable Car",
                Type = "Cable Car",
                Description = "Scenic cable car ride to Renon plateau",
                Address = "Bolzano",
                IsIndoor = false,
                Latitude = 46.4983,
                Longitude = 11.3548
            },
            new PoiDto
            {
                Id = "fallback-3",
                Name = "South Tyrol Museum of Archaeology",
                Type = "Museum",
                Description = "Home of Ötzi the Iceman",
                Address = "Bolzano",
                IsIndoor = true,
                Latitude = 46.4989,
                Longitude = 11.3508
            },
            new PoiDto
            {
                Id = "fallback-4",
                Name = "Talvera Promenade",
                Type = "Park",
                Description = "Beautiful riverside walking path",
                Address = "Bolzano",
                IsIndoor = false,
                Latitude = 46.5000,
                Longitude = 11.3450
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
