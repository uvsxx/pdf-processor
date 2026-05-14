namespace PdfProcessor.Contracts;

public sealed record PdfUploaded(
    Guid DocumentId,
    string StorageKey,
    string FileName,
    long SizeBytes);
