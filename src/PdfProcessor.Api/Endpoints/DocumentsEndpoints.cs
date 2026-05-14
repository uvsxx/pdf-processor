using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfProcessor.Application.Abstractions;
using PdfProcessor.Contracts;
using PdfProcessor.Domain;
using PdfProcessor.Infrastructure.Persistence;

namespace PdfProcessor.Api.Endpoints;

public static class DocumentsEndpoints
{
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/", UploadAsync).DisableAntiforgery();
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/content", GetContentAsync);

        return app;
    }

    private static async Task<IResult> UploadAsync(
        IFormFile? file,
        IFileStorage storage,
        AppDbContext db,
        IPublishEndpoint publisher,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });

        await using var stream = file.OpenReadStream();
        var stored = await storage.SaveAsync(stream, ct);

        var doc = Document.Create(file.FileName, stored.Key, stored.SizeBytes);
        db.Documents.Add(doc);

        await publisher.Publish(new PdfUploaded(doc.Id, stored.Key, doc.FileName, doc.SizeBytes), ct);
        await db.SaveChangesAsync(ct);

        return Results.Accepted($"/api/documents/{doc.Id}", new
        {
            id = doc.Id,
            status = doc.Status.ToString()
        });
    }

    private static async Task<Ok<List<DocumentResponse>>> ListAsync(
        AppDbContext db,
        CancellationToken ct,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(0, skip);

        var items = await db.Documents
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip).Take(take)
            .Select(x => new DocumentResponse(
                x.Id, x.FileName, x.SizeBytes, x.PageCount,
                x.Status.ToString(), x.ErrorMessage,
                x.CreatedAt, x.ProcessedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<DocumentResponse>, NotFound>> GetAsync(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new DocumentResponse(
                x.Id, x.FileName, x.SizeBytes, x.PageCount,
                x.Status.ToString(), x.ErrorMessage,
                x.CreatedAt, x.ProcessedAt))
            .FirstOrDefaultAsync(ct);

        return doc is null ? TypedResults.NotFound() : TypedResults.Ok(doc);
    }

    private static async Task<IResult> GetContentAsync(
        Guid id, AppDbContext db, CancellationToken ct)
    {
        var doc = await db.Documents
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Status, x.ErrorMessage })
            .FirstOrDefaultAsync(ct);

        if (doc is null) return Results.NotFound();

        return doc.Status switch
        {
            DocumentStatus.Pending or DocumentStatus.Processing =>
                Results.Conflict(new { status = doc.Status.ToString(), message = "document is not processed yet" }),
            DocumentStatus.Failed =>
                Results.UnprocessableEntity(new { status = "Failed", error = doc.ErrorMessage }),
            DocumentStatus.Completed =>
                await ReadContentAsync(id, db, ct),
            _ => Results.StatusCode(500)
        };
    }

    private static async Task<IResult> ReadContentAsync(Guid id, AppDbContext db, CancellationToken ct)
    {
        var content = await db.DocumentContents
            .AsNoTracking()
            .Where(x => x.DocumentId == id)
            .Select(x => x.Text)
            .FirstOrDefaultAsync(ct);

        return content is null
            ? Results.Problem("document is marked Completed but text is missing", statusCode: 500)
            : Results.Ok(new { id, text = content });
    }
}

public sealed record DocumentResponse(
    Guid Id,
    string FileName,
    long SizeBytes,
    int? PageCount,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt);
