namespace WebApplication1.Infrastructure.Storage
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
        Task DeleteFileAsync(string url, CancellationToken ct);

                                        /// <summary>
        /// Returns a stream for reading the file or null if it doesn't exist.
        /// </summary>
        Task<Stream?> OpenReadAsync(string url, CancellationToken ct);
    }
}