using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Models.Dtos.Responses;

public class AlertMetricsResponse
{
    public string AlertId { get; set; } = string.Empty;
    public int TotalAwareness { get; set; }
    public int TotalInfoReports { get; set; }
    public double GeographicReachKm { get; set; }

    public static AlertMetricsResponse FromDomain(string alertId, AlertMetrics metrics) => new()
    {
        AlertId = alertId,
        TotalAwareness = metrics.TotalAwareness,
        TotalInfoReports = metrics.TotalInfoReports,
        GeographicReachKm = metrics.GeographicReachKm
    };
}
