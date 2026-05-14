using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PdfProcessor.Application.Abstractions;
using PdfProcessor.Infrastructure.Pdf;
using PdfProcessor.Infrastructure.Persistence;
using PdfProcessor.Infrastructure.Storage;

namespace PdfProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured");

        services.AddDbContext<AppDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__migrations");
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        services.AddOptions<StorageOptions>()
            .Bind(config.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IFileStorage, FileSystemStorage>();
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();

        return services;
    }
}
