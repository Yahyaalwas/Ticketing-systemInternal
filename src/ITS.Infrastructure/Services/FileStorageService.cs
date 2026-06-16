using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Services;

/// <summary>
/// Network share-backed file storage. Swap implementation for Azure Blob / S3 without changing the interface.
/// </summary>
public sealed class FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    : IFileStorageService
{
    private readonly string _basePath = configuration["FileStorage:BasePath"]
        ?? throw new InvalidOperationException("FileStorage:BasePath configuration is required.");

    private readonly string _baseUrl = configuration["FileStorage:BaseUrl"]
        ?? throw new InvalidOperationException("FileStorage:BaseUrl configuration is required.");

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var storageKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.CreateVersion7():N}_{SanitizeFileName(fileName)}";
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        logger.LogInformation("Uploaded file {StorageKey}", storageKey);
        return storageKey;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Storage key not found: {storageKey}");

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }

    public string GetPublicUrl(string storageKey)
        => $"{_baseUrl.TrimEnd('/')}/{storageKey}";

    private static string SanitizeFileName(string fileName)
        => string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
}
