namespace PdfProcessor.Application.Abstractions;

public sealed record ExtractedText(string Text, int PageCount);

public interface IPdfTextExtractor
{
    ExtractedText Extract(Stream pdfStream);
}

public sealed class InvalidPdfException : Exception
{
    public InvalidPdfException(string message, Exception? inner = null) : base(message, inner) { }
}
