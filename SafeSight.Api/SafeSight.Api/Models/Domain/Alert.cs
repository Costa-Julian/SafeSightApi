using System.ComponentModel.DataAnnotations;

namespace SafeSight.Api.Models.Domain;

public class Alert
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public MissingPerson MissingPerson { get; set; } = new();
    public string Situation { get; set; } = string.Empty;
    public GeoPoint LastKnownLocation { get; set; } = new();
    public DateTime DisappearanceDate { get; set; }
    // TODO: reemplazar por identificación real de emisor con autenticación cuando el sistema evolucione
    public int EmitterId { get; set; }
    public DateTime EmittedAt { get; set; }
    public AlertStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
