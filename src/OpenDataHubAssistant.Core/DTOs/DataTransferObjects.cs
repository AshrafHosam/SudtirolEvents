using OpenDataHubAssistant.Core.Enums;

namespace OpenDataHubAssistant.Core.DTOs;

/// <summary>
/// Weather data transfer object
/// </summary>
public class WeatherDto
{
    public double TemperatureC { get; set; }
    public double PrecipitationMm { get; set; }
    public double WindKph { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>
/// Weather response with classification and recommendations
/// </summary>
public class WeatherResponseDto
{
    public WeatherDto Weather { get; set; } = new();
    public List<WeatherClassification> Classifications { get; set; } = new();
    public List<ActivityRecommendationDto> Recommendations { get; set; } = new();
}

/// <summary>
/// Activity recommendation DTO
/// </summary>
public class ActivityRecommendationDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsIndoor { get; set; }
    public string Type { get; set; } = string.Empty; // "Event" or "POI"
    public string? Explanation { get; set; }
}

/// <summary>
/// Location DTO
/// </summary>
public class LocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>
/// Event DTO
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsIndoor { get; set; }
}

/// <summary>
/// Point of Interest DTO
/// </summary>
public class PoiDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsIndoor { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
/// Full recommendation response including LLM explanation
/// </summary>
public class RecommendationResponseDto
{
    public string Explanation { get; set; } = string.Empty;
    public List<ActivityRecommendationDto> Recommendations { get; set; } = new();
    public WeatherDto? Weather { get; set; }
    public List<WeatherClassification> Classifications { get; set; } = new();
    public List<EventDto> SourceEvents { get; set; } = new();
    public List<PoiDto> SourcePois { get; set; } = new();
}

/// <summary>
/// Chat request DTO
/// </summary>
public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Chat response DTO
/// </summary>
public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public RecommendationResponseDto? Data { get; set; }
}
