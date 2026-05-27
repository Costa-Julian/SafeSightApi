namespace SafeSight.Api.Models.Domain;

public class MissingPerson
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string PhysicalDescription { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
}
