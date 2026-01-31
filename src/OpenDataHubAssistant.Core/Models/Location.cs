using System.ComponentModel.DataAnnotations;

namespace OpenDataHubAssistant.Core.Models;

/// <summary>
/// Represents a geographic location
/// </summary>
public class Location
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public virtual ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();

    public virtual ICollection<RecommendationLog> RecommendationLogs { get; set; } = new List<RecommendationLog>();
}
