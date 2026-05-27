using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafeSight.Api.Models.Configuration;
using SafeSight.Api.Services.Interfaces;

namespace SafeSight.Api.Services;

public class PhotoStorageService : IPhotoStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private readonly string _storagePath;
    private readonly long _maxFileSizeBytes;
    private readonly ILogger<PhotoStorageService> _logger;

    public PhotoStorageService(IWebHostEnvironment env, IOptions<PhotoSettings> settings, ILogger<PhotoStorageService> logger)
    {
        _storagePath = Path.Combine(env.WebRootPath, "photos");
        _maxFileSizeBytes = settings.Value.MaxFileSizeMb * 1024L * 1024L;
        _logger = logger;

        // TODO: en producción, mover almacenamiento de fotos a un blob storage o servicio dedicado.
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveAsync(IFormFile photo)
    {
        string extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Tipo de archivo no permitido: {extension}. Solo se aceptan jpg y png.");
        }

        if (photo.Length > _maxFileSizeBytes)
        {
            throw new InvalidOperationException($"El archivo supera el tamaño máximo permitido de {_maxFileSizeBytes / (1024 * 1024)} MB.");
        }

        string fileName = $"{Guid.NewGuid()}{extension}";
        string fullPath = Path.Combine(_storagePath, fileName);

        await using FileStream stream = new FileStream(fullPath, FileMode.Create);
        await photo.CopyToAsync(stream);

        _logger.LogInformation("Foto guardada: {FileName}", fileName);
        return $"/photos/{fileName}";
    }
}
