using MassTransit;
using Microsoft.EntityFrameworkCore;
using PdfProcessor.Contracts;
using PdfProcessor.Infrastructure.Persistence;

namespace PdfProcessor.Worker.Consumers;

public sealed class PdfUploadedFaultConsumer : IConsumer<Fault<PdfUploaded>>
{
    private readonly AppDbContext _db;
    private readonly ILogger<PdfUploadedFaultConsumer> _logger;

    public PdfUploadedFaultConsumer(AppDbContext db, ILogger<PdfUploadedFaultConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<PdfUploaded>> ctx)
    {
        var docId = ctx.Message.Message.DocumentId;
        var doc = await _db.Documents.FirstOrDefaultAsync(x => x.Id == docId, ctx.CancellationToken);
        if (doc is null) return;

        var reason = string.Join(" | ", ctx.Message.Exceptions.Select(e => $"{e.ExceptionType}: {e.Message}"));
        _logger.LogError("Document {DocumentId} failed permanently: {Reason}", docId, reason);

        doc.MarkFailed(reason);
        await _db.SaveChangesAsync(ctx.CancellationToken);
    }
}
