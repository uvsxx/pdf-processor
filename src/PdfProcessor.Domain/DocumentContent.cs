namespace PdfProcessor.Domain;

public sealed class DocumentContent
{
    public Guid DocumentId { get; private set; }
    public string Text { get; private set; } = default!;

    private DocumentContent() { }

    internal DocumentContent(Guid documentId, string text)
    {
        DocumentId = documentId;
        Text = text;
    }
}
