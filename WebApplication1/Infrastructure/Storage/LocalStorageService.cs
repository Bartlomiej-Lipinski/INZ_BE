namespace WebApplication1.Infrastructure.Storage
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _uploadsRoot;
        private readonly string _baseRequestPath;
        private readonly ILogger<LocalStorageService> _logger;
        private readonly IWebHostEnvironment _env;

        public LocalStorageService(IConfiguration config, IWebHostEnvironment env, ILogger<LocalStorageService> logger)
        {
            _env = env;
            _logger = logger;
            var folder = config["Storage:UploadsFolder"] ?? "uploads";
            _uploadsRoot = Path.IsPathRooted(folder) 
                ? folder 
                                : Path.Combine(_env.ContentRootPath, folder); // Use ContentRootPath instead of wwwroot
            _baseRequestPath = config["Storage:BaseRequestPath"] ?? "/api/storage";
            Directory.CreateDirectory(_uploadsRoot);
        }

        private string? GetSafeFilePath(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(_baseRequestPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid URL format: {Url}", url);
                return null;
            }

            var fileName = Path.GetFileName(url);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Empty filename in URL: {Url}", url);
                return null;
            }

            // Sprawdź nieprawidłowe znaki w nazwie pliku
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                _logger.LogWarning("Invalid characters in filename: {FileName}", fileName);
                return null;
            }

            // Zbuduj pełną ścieżkę i sprawdź, czy znajduje się w dozwolonym katalogu
            var filePath = Path.Combine(_uploadsRoot, fileName);
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve path for URL: {Url}", url);
                return null;
            }

            // Normalizuj ścieżki dla porównania
            var normalizedRoot = Path.GetFullPath(_uploadsRoot);
            if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !fullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {Url} -> {FullPath}", url, fullPath);
                return null;
            }

            return fullPath;
        }

        public async Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
        {
            var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(_uploadsRoot, safeName);

            using (var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs, ct);
            }

            var url = $"{_baseRequestPath}/{safeName}";
            _logger.LogInformation("Saved file to {Path} as {Url}", filePath, url);
            return url;
        }

        public Task DeleteFileAsync(string url, CancellationToken ct)
        {
            try
            {
                var filePath = GetSafeFilePath(url);
                if (filePath != null && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file {Url}", url);
            }

            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string url, CancellationToken ct)
        {
            try
            {
                var filePath = GetSafeFilePath(url);
                if (filePath == null || !File.Exists(filePath))
                {
                    return Task.FromResult<Stream?>(null);
                }

                var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Task.FromResult<Stream?>(fs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open file {Url}", url);
                return Task.FromResult<Stream?>(null);
            }
        }
    }
}