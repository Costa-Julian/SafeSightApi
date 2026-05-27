using Microsoft.AspNetCore.Http;

namespace SafeSight.Api.Models.Dtos.Requests;

public class InfoReportRequest
{
    public string AlertId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public IFormFile? Photo { get; set; }
}
