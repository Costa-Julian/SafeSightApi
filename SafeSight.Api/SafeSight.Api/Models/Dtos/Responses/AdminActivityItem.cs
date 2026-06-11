namespace SafeSight.Api.Models.Dtos.Responses;

public class AdminActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AlertId { get; set; }
}
