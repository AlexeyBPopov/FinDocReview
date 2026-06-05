using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace FinDocReview.Infrastructure.Services;

public class LocalStorageService
{
    private readonly string _storageType;
    private readonly string _basePath;
    private readonly string? _blobConnectionString;
    private readonly string? _containerName;

    public LocalStorageService(IConfiguration configuration)
    {
        _storageType = configuration["Storage:Type"] ?? "Local";
        _basePath = configuration["Storage:LocalPath"] ?? "App_Data/uploads";
        _blobConnectionString = configuration["Storage:BlobConnectionString"];
        _containerName = configuration["Storage:ContainerName"] ?? "documents";

        if (_storageType == "Local")
            Directory.CreateDirectory(_basePath);
    }

    public async Task<(string? localPath, string? blobUri)> SaveFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken ct = default)
    {
        if (_storageType == "Azure")
        {
            var blobUri = await SaveToBlobAsync(fileStream, fileName, ct);
            return (null, blobUri);
        }
        else
        {
            var localPath = await SaveToLocalAsync(fileStream, fileName, ct);
            return (localPath, null);
        }
    }

    private async Task<string> SaveToBlobAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var blobName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var containerClient = new BlobContainerClient(_blobConnectionString, _containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken: ct);
        return blobClient.Uri.ToString();
    }

    private async Task<string> SaveToLocalAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_basePath, safeFileName);
        await using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output, ct);
        return filePath;
    }

    public async Task<Stream> GetFileAsync(string? filePath, string? blobUri = null)
    {
        if (_storageType == "Azure" && !string.IsNullOrEmpty(blobUri))
        {
            var blobName = new Uri(blobUri).Segments.Last();
            var containerClient = new BlobContainerClient(_blobConnectionString, _containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }

        return await Task.FromResult(File.OpenRead(filePath!));
    }

    public async Task DeleteFileAsync(string? localPath, string? blobUri = null)
    {
        if (_storageType == "Azure" && !string.IsNullOrEmpty(blobUri))
        {
            var blobName = new Uri(blobUri).Segments.Last();
            var containerClient = new BlobContainerClient(_blobConnectionString, _containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
        else if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
        {
            File.Delete(localPath);
        }
    }
}