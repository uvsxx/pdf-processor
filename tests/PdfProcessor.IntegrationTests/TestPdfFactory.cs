using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PdfProcessor.IntegrationTests;

public static class TestPdfFactory
{
    static TestPdfFactory()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] CreatePdf(string text)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Text(text).FontSize(14);
            });
        }).GeneratePdf();
    }
}
