using System.ComponentModel.DataAnnotations;

namespace SafeSight.Api.Models.Domain;

public class InfoReportDocument
{
    [Key]
    public Guid Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime ReportedAt { get; set; }
}
