using Microsoft.AspNetCore.Http;

namespace SafeSight.Api.Models.Dtos.Requests;

public class CreateAlertRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string PhysicalDescription { get; set; } = string.Empty;
    public string Situation { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime DisappearanceDate { get; set; }
    // 1 = ciudadano, 2 = entidad
    public int EmitterId { get; set; }
    public IFormFile? Photo { get; set; }
}
