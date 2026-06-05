using SafeSight.Api.Models.Dtos.Responses;

namespace SafeSight.Api.Services.Interfaces;

public interface IFcmService
{
    Task SendNewAlertAsync(AlertResponse alert);
}
