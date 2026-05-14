using System.Text;
using PdfProcessor.Application.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace PdfProcessor.Infrastructure.Pdf;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public ExtractedText Extract(Stream pdfStream)
    {
        try
        {
            using var pdf = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();
            var pageCount = 0;
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
                pageCount++;
            }
            // Postgres text не принимает 0x00 байты, PdfPig иногда их возвращает.
            var text = sb.ToString().Replace("\0", string.Empty);
            return new ExtractedText(text, pageCount);
        }
        catch (PdfDocumentEncryptedException ex)
        {
            throw new InvalidPdfException("PDF is encrypted", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidPdfException($"Failed to parse PDF: {ex.Message}", ex);
        }
    }
}
