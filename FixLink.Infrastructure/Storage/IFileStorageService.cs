namespace FixLink.Infrastructure.Storage;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}