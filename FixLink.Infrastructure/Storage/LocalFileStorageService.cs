
namespace FixLink.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(string storagePath)
    {
        _storagePath = storagePath;
    }

    public async Task<string> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_storagePath);

        // Only allow known image types.
        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException(
                "Unsupported image type.")
        };

        // Generate a safe, unique filename.
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_storagePath, storedFileName);

        await using var outputStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await fileStream.CopyToAsync(
            outputStream,
            cancellationToken);

        return $"/uploads/providers/{storedFileName}";
    }

    public Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Prevent directory traversal or deleting outside this folder.
        var safeFileName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return Task.CompletedTask;
        }

        var filePath = Path.Combine(_storagePath, safeFileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
