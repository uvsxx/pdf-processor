namespace PdfProcessor.Domain;

public enum DocumentStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
