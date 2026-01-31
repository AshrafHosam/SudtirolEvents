using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDataHubAssistant.Core.Models;

/// <summary>
/// Represents a snapshot of weather data at a specific time
/// </summary>
public class WeatherSnapshot
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Location))]
    public int LocationId { get; set; }

    public DateTime Timestamp { get; set; }

    public double TemperatureC { get; set; }

    public double PrecipitationMm { get; set; }

    public double WindKph { get; set; }

    [MaxLength(200)]
    public string ConditionText { get; set; } = string.Empty;

    public string? RawJson { get; set; }

    public virtual Location? Location { get; set; }
}
