using Microsoft.Extensions.Options;
using PdfProcessor.Application.Abstractions;

namespace PdfProcessor.Infrastructure.Storage;

public sealed class FileSystemStorage : IFileStorage
{
    private readonly string _rootPath;

    public FileSystemStorage(IOptions<StorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(Stream content, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var subDir = Path.Combine(now.Year.ToString("D4"), now.Month.ToString("D2"), now.Day.ToString("D2"));
        var fileName = $"{Guid.NewGuid():N}.pdf";
        var key = Path.Combine(subDir, fileName).Replace('\\', '/');
        var fullPath = Path.Combine(_rootPath, subDir, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, ct);
        }

        var size = new FileInfo(fullPath).Length;
        return new StoredFile(key, size);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, key);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

}
