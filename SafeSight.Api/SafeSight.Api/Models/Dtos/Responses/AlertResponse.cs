using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Models.Dtos.Responses;

public class AlertResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string PhysicalDescription { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public string Situation { get; set; } = string.Empty;
    public double LastKnownLatitude { get; set; }
    public double LastKnownLongitude { get; set; }
    public DateTime DisappearanceDate { get; set; }
    public int EmitterId { get; set; }
    public DateTime EmittedAt { get; set; }
    public AlertStatus Status { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public static AlertResponse FromDomain(Alert alert) => new()
    {
        Id = alert.Id,
        FirstName = alert.MissingPerson.FirstName,
        LastName = alert.MissingPerson.LastName,
        Age = alert.MissingPerson.Age,
        PhysicalDescription = alert.MissingPerson.PhysicalDescription,
        PhotoUrl = alert.MissingPerson.PhotoUrl,
        Situation = alert.Situation,
        LastKnownLatitude = alert.LastKnownLocation.Latitude,
        LastKnownLongitude = alert.LastKnownLocation.Longitude,
        DisappearanceDate = alert.DisappearanceDate,
        EmitterId = alert.EmitterId,
        EmittedAt = alert.EmittedAt,
        Status = alert.Status,
        ResolvedAt = alert.ResolvedAt
    };
}
