using System.ComponentModel.DataAnnotations;

namespace SafeSight.Api.Models.Domain;

public class HeatmapCell
{
    // Id = geohash string de la celda (ej: "-34.60:-58.38")
    [Key]
    public string Id { get; set; } = string.Empty;
    public string? AlertId { get; set; }
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public int AwarenessCount { get; set; }
    public int InfoCount { get; set; }
    // Suma ponderada: InfoCount * InfoWeight + AwarenessCount * AwarenessWeight
    public double WeightedIntensity { get; set; }
    public DateTime LastUpdated { get; set; }
}
