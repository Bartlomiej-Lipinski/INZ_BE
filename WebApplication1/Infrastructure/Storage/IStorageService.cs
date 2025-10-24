namespace WebApplication1.Infrastructure.Storage
{
    public interface IStorageService
    {
        /// <summary>
        /// Saves a file to the storage system.
        /// </summary>
        /// <param name="stream">The stream containing the file data.</param>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>The URL of the saved file.</returns>
        Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, CancellationToken ct);

        /// <summary>
        /// Deletes a file from the storage system.
        /// </summary>
        /// <param name="url">The URL of the file to delete.</param>
        /// <param name="ct">A cancellation token.</param>
        Task DeleteFileAsync(string url, CancellationToken ct);

        /// <summary>
        /// Returns a stream for reading the file or null if it doesn't exist.
        /// </summary>
        Task<Stream?> OpenReadAsync(string url, CancellationToken ct);
    }
}