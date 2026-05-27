using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Models.Dtos.Requests;

public class UpdateAlertStatusRequest
{
    public AlertStatus Status { get; set; }
}
