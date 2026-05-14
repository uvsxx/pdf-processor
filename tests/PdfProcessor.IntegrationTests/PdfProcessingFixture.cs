using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PdfProcessor.Infrastructure.Persistence;
using PdfProcessor.Worker;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

using HostingHost = Microsoft.Extensions.Hosting.Host;

namespace PdfProcessor.IntegrationTests;

public sealed class PdfProcessingFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pdf_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    private string _filesDir = string.Empty;
    private IHost? _workerHost;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_pg.StartAsync(), _rabbit.StartAsync());

        _filesDir = Path.Combine(Path.GetTempPath(), "pdftests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_filesDir);

        // Схему создаём заранее, чтобы Worker не пошёл в несуществующие таблицы Outbox/Inbox.
        var ctxOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;
        await using (var ctx = new AppDbContext(ctxOptions))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        _ = Server;

        var workerBuilder = HostingHost.CreateApplicationBuilder();
        workerBuilder.Configuration.AddInMemoryCollection(GetTestSettings()!);
        WorkerHostBuilder.Configure(workerBuilder);

        _workerHost = workerBuilder.Build();
        await _workerHost.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ConfigurationManager в Minimal API не подхватывает providers из ConfigureAppConfiguration,
        // поэтому пробрасываем настройки через UseSetting.
        foreach (var (k, v) in GetTestSettings())
            builder.UseSetting(k, v);
    }

    private Dictionary<string, string?> GetTestSettings() => new()
    {
        ["ConnectionStrings:Postgres"] = _pg.GetConnectionString(),
        ["RabbitMq:Host"] = _rabbit.Hostname,
        ["RabbitMq:Port"] = _rabbit.GetMappedPublicPort(5672).ToString(),
        ["RabbitMq:Username"] = "guest",
        ["RabbitMq:Password"] = "guest",
        ["Storage:RootPath"] = _filesDir,
    };

    public new async Task DisposeAsync()
    {
        if (_workerHost is not null)
        {
            await _workerHost.StopAsync();
            _workerHost.Dispose();
        }

        await base.DisposeAsync();
        await _pg.DisposeAsync();
        await _rabbit.DisposeAsync();

        try
        {
            if (Directory.Exists(_filesDir))
                Directory.Delete(_filesDir, recursive: true);
        }
        catch (IOException) { }
    }
}
