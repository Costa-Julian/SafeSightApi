namespace SafeSight.Api.Models.Dtos.Responses;

public class AdminReportRow
{
    public string ReportId { get; set; } = string.Empty;
    public string AlertId { get; set; } = string.Empty;
    public string AlertName { get; set; } = string.Empty;
    public int AlertAge { get; set; }
    public int EmitterId { get; set; }
    public string AlertStatus { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ReportedAt { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public double? MinutesToResolution { get; set; }
}
