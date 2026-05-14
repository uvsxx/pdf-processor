using MassTransit;
using Microsoft.EntityFrameworkCore;
using PdfProcessor.Application.Abstractions;
using PdfProcessor.Contracts;
using PdfProcessor.Domain;
using PdfProcessor.Infrastructure.Persistence;

namespace PdfProcessor.Worker.Consumers;

public sealed class PdfUploadedConsumer : IConsumer<PdfUploaded>
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IPdfTextExtractor _extractor;
    private readonly ILogger<PdfUploadedConsumer> _logger;

    public PdfUploadedConsumer(
        AppDbContext db,
        IFileStorage storage,
        IPdfTextExtractor extractor,
        ILogger<PdfUploadedConsumer> logger)
    {
        _db = db;
        _storage = storage;
        _extractor = extractor;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PdfUploaded> ctx)
    {
        var msg = ctx.Message;
        var ct = ctx.CancellationToken;

        var doc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == msg.DocumentId, ct);
        if (doc is null)
        {
            _logger.LogWarning("Document {DocumentId} not yet visible, will retry", msg.DocumentId);
            throw new InvalidOperationException($"Document {msg.DocumentId} not found");
        }

        if (doc.Status == DocumentStatus.Completed)
        {
            _logger.LogInformation("Document {DocumentId} already completed, skipping", msg.DocumentId);
            return;
        }

        doc.MarkProcessing();
        await _db.SaveChangesAsync(ct);

        try
        {
            await using var stream = await _storage.OpenReadAsync(msg.StorageKey, ct);
            var extracted = _extractor.Extract(stream);

            doc.MarkCompleted(extracted.Text, extracted.PageCount);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Document {DocumentId} processed: {Pages} pages, {Chars} chars",
                doc.Id, extracted.PageCount, extracted.Text.Length);
        }
        catch (InvalidPdfException ex)
        {
            _logger.LogWarning(ex, "Document {DocumentId} is invalid, marking as Failed", doc.Id);
            doc.MarkFailed(ex.Message);
            await _db.SaveChangesAsync(ct);
        }
    }
}
