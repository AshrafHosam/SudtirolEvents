using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDataHubAssistant.Core.Models;

/// <summary>
/// Logs recommendation requests and responses for analysis
/// </summary>
public class RecommendationLog
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Location))]
    public int? LocationId { get; set; }

    public DateTime Timestamp { get; set; }

    [MaxLength(1000)]
    public string? QueryText { get; set; }

    [MaxLength(4000)]
    public string? RecommendationText { get; set; }

    [MaxLength(4000)]
    public string? SourceDataSummary { get; set; }

    public virtual Location? Location { get; set; }
}
