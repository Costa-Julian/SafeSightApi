namespace SafeSight.Api.Models.Dtos.Requests;

public class AwarenessReportRequest
{
    public string AlertId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ReportedAt { get; set; }
}
