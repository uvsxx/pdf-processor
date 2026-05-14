namespace PdfProcessor.Domain;

public sealed class Document
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = default!;
    public string StorageKey { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public int? PageCount { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    public DocumentContent? Content { get; private set; }

    private Document() { }

    public static Document Create(string fileName, string storageKey, long sizeBytes)
    {
        var now = DateTimeOffset.UtcNow;
        return new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            StorageKey = storageKey,
            SizeBytes = sizeBytes,
            Status = DocumentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
        ErrorMessage = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string text, int pageCount)
    {
        Status = DocumentStatus.Completed;
        PageCount = pageCount;
        ErrorMessage = null;
        Content = new DocumentContent(Id, text);
        ProcessedAt = DateTimeOffset.UtcNow;
        UpdatedAt = ProcessedAt.Value;
    }

    public void MarkFailed(string reason)
    {
        Status = DocumentStatus.Failed;
        ErrorMessage = Truncate(reason, 2000);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
