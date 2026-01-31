using OpenDataHubAssistant.Core.DTOs;
using OpenDataHubAssistant.Core.Enums;
using OpenDataHubAssistant.Core.Interfaces;

namespace OpenDataHubAssistant.Core.Services;

/// <summary>
/// Service for generating activity recommendations based on weather, events, and POIs
/// </summary>
public class ActivityRecommendationService : IActivityRecommendationService
{
    private readonly IWeatherInterpreterService _weatherInterpreter;

    public ActivityRecommendationService(IWeatherInterpreterService weatherInterpreter)
    {
        _weatherInterpreter = weatherInterpreter;
    }

    /// <inheritdoc />
    public Task<List<ActivityRecommendationDto>> GetRecommendationsAsync(
        WeatherDto weather,
        List<WeatherClassification> classifications,
        IEnumerable<EventDto> events,
        IEnumerable<PoiDto> pois,
        int maxRecommendations = 5)
    {
        var recommendations = new List<ActivityRecommendationDto>();
        bool preferIndoor = classifications.Contains(WeatherClassification.Bad);

        // Process events
        var eventList = events.ToList();
        var sortedEvents = preferIndoor
            ? eventList.OrderByDescending(e => e.IsIndoor).ThenBy(e => e.StartDate)
            : eventList.OrderBy(e => e.IsIndoor).ThenBy(e => e.StartDate);

        foreach (var evt in sortedEvents.Take(maxRecommendations))
        {
            recommendations.Add(new ActivityRecommendationDto
            {
                Name = evt.Name,
                Description = evt.Description ?? $"Event at {evt.Location}",
                IsIndoor = evt.IsIndoor,
                Type = "Event",
                Explanation = GenerateBasicExplanation(evt.Name, evt.IsIndoor, preferIndoor)
            });
        }

        // Fill remaining slots with POIs
        int remainingSlots = maxRecommendations - recommendations.Count;
        if (remainingSlots > 0)
        {
            var poiList = pois.ToList();
            var sortedPois = preferIndoor
                ? poiList.OrderByDescending(p => p.IsIndoor)
                : poiList.OrderBy(p => p.IsIndoor);

            foreach (var poi in sortedPois.Take(remainingSlots))
            {
                recommendations.Add(new ActivityRecommendationDto
                {
                    Name = poi.Name,
                    Description = poi.Description ?? $"{poi.Type} at {poi.Address}",
                    IsIndoor = poi.IsIndoor,
                    Type = "POI",
                    Explanation = GenerateBasicExplanation(poi.Name, poi.IsIndoor, preferIndoor)
                });
            }
        }

        return Task.FromResult(recommendations.Take(maxRecommendations).ToList());
    }

    private static string GenerateBasicExplanation(string name, bool isIndoor, bool preferIndoor)
    {
        if (preferIndoor && isIndoor)
        {
            return $"Recommended because it's an indoor activity, suitable for current weather conditions.";
        }
        else if (!preferIndoor && !isIndoor)
        {
            return $"Great for enjoying the good weather outdoors.";
        }
        else if (preferIndoor && !isIndoor)
        {
            return $"Outdoor activity - check weather conditions before visiting.";
        }
        else
        {
            return $"Indoor option available if weather changes.";
        }
    }
}
