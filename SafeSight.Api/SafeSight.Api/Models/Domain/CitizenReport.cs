namespace SafeSight.Api.Models.Domain;

public class CitizenReport
{
    public Guid Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ReportedAt { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
}
