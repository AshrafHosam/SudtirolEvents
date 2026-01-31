using System.ComponentModel.DataAnnotations;

namespace OpenDataHubAssistant.Core.Models;

/// <summary>
/// Represents an event from Open Data Hub
/// </summary>
public class Event
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    public bool IsIndoor { get; set; }

    /// <summary>
    /// External ID from Open Data Hub
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }
}
