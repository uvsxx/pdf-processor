using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace PdfProcessor.IntegrationTests;

public sealed class SmokeTests : IClassFixture<PdfProcessingFixture>
{
    private readonly PdfProcessingFixture _fx;

    public SmokeTests(PdfProcessingFixture fx) => _fx = fx;

    [Fact]
    public async Task UploadedPdf_GetsProcessedAndContentReturned()
    {
        var client = _fx.CreateClient();
        var pdfBytes = TestPdfFactory.CreatePdf("Hello PdfProcessor World!\nThis is a sample PDF for smoke and manual testing.");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new("application/pdf");
        form.Add(fileContent, "file", "smoke.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", form);
        uploadResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var created = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var deadline = DateTime.UtcNow.AddSeconds(60);
        HttpResponseMessage? contentResponse = null;
        while (DateTime.UtcNow < deadline)
        {
            contentResponse = await client.GetAsync($"/api/documents/{id}/content");
            if (contentResponse.StatusCode == HttpStatusCode.OK)
                break;
            await Task.Delay(500);
        }

        contentResponse.ShouldNotBeNull();
        contentResponse!.StatusCode.ShouldBe(HttpStatusCode.OK, "document was not processed within 60s");

        var body = await contentResponse.Content.ReadAsStringAsync();
        body.ShouldContain("Hello PdfProcessor World!");
    }
}
