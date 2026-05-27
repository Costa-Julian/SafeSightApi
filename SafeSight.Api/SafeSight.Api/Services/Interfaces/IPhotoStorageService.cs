using Microsoft.AspNetCore.Http;

namespace SafeSight.Api.Services.Interfaces;

public interface IPhotoStorageService
{
    Task<string> SaveAsync(IFormFile photo);
}
