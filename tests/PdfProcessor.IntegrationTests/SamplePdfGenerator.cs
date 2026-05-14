using Xunit;

namespace PdfProcessor.IntegrationTests;

/// <summary>
/// Кладёт sample.pdf в корень репозитория, чтобы примеры curl из README сразу работали.
/// </summary>
public sealed class SamplePdfGenerator
{
    [Fact]
    public void EnsureSamplePdfExists()
    {
        var repoRoot = FindRepoRoot();
        var samplePath = Path.Combine(repoRoot, "sample.pdf");

        var bytes = TestPdfFactory.CreatePdf(
            "Hello PdfProcessor!\n\n" +
            "Этот файл сгенерирован тестом SamplePdfGenerator.EnsureSamplePdfExists\n" +
            "и используется как фикстура для smoke-проверки pipeline'а:\n" +
            "POST /api/documents → RabbitMQ → Worker → PostgreSQL → GET /content.\n");

        File.WriteAllBytes(samplePath, bytes);
        Assert.True(File.Exists(samplePath));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PdfProcessor.sln")))
            dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("PdfProcessor.sln not found above test bin");
        return dir.FullName;
    }
}
