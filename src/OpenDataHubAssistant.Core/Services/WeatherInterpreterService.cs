using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Enums;
using OpenDataHubAssistant.Core.Interfaces;

namespace OpenDataHubAssistant.Core.Services;

/// <summary>
/// Service for interpreting and classifying weather conditions using simple rules
/// </summary>
public class WeatherInterpreterService : IWeatherInterpreterService
{
    // Configurable thresholds
    private const double ColdThresholdC = 10.0;
    private const double HotThresholdC = 30.0;
    private const double WindyThresholdKph = 30.0;
    private const double RainyThresholdMm = 0.5;

    /// <inheritdoc />
    public List<WeatherClassification> ClassifyWeather(WeatherDto weather)
    {
        var classifications = new List<WeatherClassification>();

        // Check temperature
        if (weather.TemperatureC < ColdThresholdC)
        {
            classifications.Add(WeatherClassification.Cold);
        }
        else if (weather.TemperatureC > HotThresholdC)
        {
            classifications.Add(WeatherClassification.Hot);
        }

        // Check precipitation
        if (weather.PrecipitationMm > RainyThresholdMm)
        {
            classifications.Add(WeatherClassification.Rainy);
        }

        // Check wind
        if (weather.WindKph > WindyThresholdKph)
        {
            classifications.Add(WeatherClassification.Windy);
        }

        // Determine overall good/bad
        bool isBad = classifications.Contains(WeatherClassification.Rainy) ||
                     classifications.Contains(WeatherClassification.Cold) ||
                     classifications.Contains(WeatherClassification.Windy);

        if (isBad)
        {
            classifications.Insert(0, WeatherClassification.Bad);
        }
        else
        {
            classifications.Insert(0, WeatherClassification.Good);
        }

        return classifications;
    }

    /// <inheritdoc />
    public bool IsSuitableForOutdoor(WeatherDto weather)
    {
        var classifications = ClassifyWeather(weather);
        return classifications.Contains(WeatherClassification.Good);
    }

    /// <inheritdoc />
    public string GetClassificationSummary(List<WeatherClassification> classifications)
    {
        if (classifications.Count == 0)
            return "Unknown weather conditions";

        var primary = classifications[0];
        var details = classifications.Skip(1).ToList();

        string summary = primary == WeatherClassification.Good
            ? "Good weather for activities"
            : "Weather conditions may limit outdoor activities";

        if (details.Count > 0)
        {
            var detailStrings = details.Select(c => c switch
            {
                WeatherClassification.Cold => "cold temperatures",
                WeatherClassification.Hot => "high temperatures",
                WeatherClassification.Rainy => "rain expected",
                WeatherClassification.Windy => "strong winds",
                _ => c.ToString().ToLower()
            });
            summary += $" ({string.Join(", ", detailStrings)})";
        }

        return summary;
    }
}
