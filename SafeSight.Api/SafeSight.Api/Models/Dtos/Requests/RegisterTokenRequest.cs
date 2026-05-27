namespace SafeSight.Api.Models.Dtos.Requests;

public class RegisterTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
