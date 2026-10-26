namespace App.Application.Common.Interfaces.Services
{
    public interface IFileStorageService
    {
        // Persists the file and returns its public URL.
        Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

        // Best-effort removal of a previously stored file; implementations must not throw on missing files.
        Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
    }
}
