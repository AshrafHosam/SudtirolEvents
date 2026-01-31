namespace OpenDataHubAssistant.Infrastructure.Configuration;

/// <summary>
/// Configuration for Open Data Hub API clients
/// </summary>
public class OpenDataHubSettings
{
    public const string SectionName = "OpenDataHub";

    /// <summary>
    /// Base URL for weather API
    /// </summary>
    public string WeatherApiBaseUrl { get; set; } = "https://tourism.opendatahub.com/v1/Weather";

    /// <summary>
    /// Base URL for events API
    /// </summary>
    public string EventsApiBaseUrl { get; set; } = "https://tourism.opendatahub.com/v1/Event";

    /// <summary>
    /// Base URL for POI API
    /// </summary>
    public string PoiApiBaseUrl { get; set; } = "https://tourism.opendatahub.com/v1/ODHActivityPoi";

    /// <summary>
    /// Cache TTL in minutes
    /// </summary>
    public int CacheTtlMinutes { get; set; } = 15;

    /// <summary>
    /// HTTP timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;
}

/// <summary>
/// Configuration for OpenAI service
/// </summary>
public class OpenAiSettings
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// OpenAI API key - should be set via environment variable OPENAI_API_KEY
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use (e.g., gpt-4, gpt-3.5-turbo)
    /// </summary>
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Base URL for OpenAI API (can be overridden for Azure OpenAI)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for response generation (0-2)
    /// </summary>
    public double Temperature { get; set; } = 0.7;
}
