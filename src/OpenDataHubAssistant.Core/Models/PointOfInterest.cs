using System.ComponentModel.DataAnnotations;

namespace OpenDataHubAssistant.Core.Models;

/// <summary>
/// Represents a Point of Interest from Open Data Hub
/// </summary>
public class PointOfInterest
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Type { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsIndoor { get; set; }

    /// <summary>
    /// External ID from Open Data Hub
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
