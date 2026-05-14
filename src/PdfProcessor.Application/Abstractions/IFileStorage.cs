namespace PdfProcessor.Application.Abstractions;

public sealed record StoredFile(string Key, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, CancellationToken ct);
    Task<Stream> OpenReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
