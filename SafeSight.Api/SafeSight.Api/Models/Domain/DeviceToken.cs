using MongoDB.Bson;

namespace SafeSight.Api.Models.Domain;

public class DeviceToken
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Token { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
