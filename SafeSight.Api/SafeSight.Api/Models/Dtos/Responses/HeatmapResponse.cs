using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Models.Dtos.Responses;

public class HeatmapResponse
{
    public string? AlertId { get; set; }
    public List<HeatmapCellDto> Cells { get; set; } = new();
}

public class HeatmapCellDto
{
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public int AwarenessCount { get; set; }
    public int InfoCount { get; set; }
    public double WeightedIntensity { get; set; }
    public DateTime LastUpdated { get; set; }

    public static HeatmapCellDto FromDomain(HeatmapCell cell) => new()
    {
        CenterLatitude = cell.CenterLatitude,
        CenterLongitude = cell.CenterLongitude,
        AwarenessCount = cell.AwarenessCount,
        InfoCount = cell.InfoCount,
        WeightedIntensity = cell.WeightedIntensity,
        LastUpdated = cell.LastUpdated
    };
}
