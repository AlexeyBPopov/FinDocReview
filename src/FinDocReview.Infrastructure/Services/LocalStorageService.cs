using Microsoft.Extensions.Configuration;

namespace FinDocReview.Infrastructure.Services;

public class LocalStorageService
{
    private readonly string _basePath;

    public LocalStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Storage:LocalPath"] ?? "App_Data/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_basePath, safeFileName);
        await using var output = File.Create(filePath);
        await fileStream.CopyToAsync(output, ct);
        return filePath;
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        return await Task.FromResult(File.OpenRead(filePath));
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}