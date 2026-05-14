using MassTransit;
using Microsoft.Extensions.Hosting;
using PdfProcessor.Infrastructure;
using PdfProcessor.Infrastructure.Persistence;
using PdfProcessor.Worker.Consumers;
using Serilog;

namespace PdfProcessor.Worker;

public static class WorkerHostBuilder
{
    public static void Configure(IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((sp, lc) => lc.ReadFrom.Configuration(builder.Configuration));

        builder.Services.AddInfrastructure(builder.Configuration);

        var rabbit = builder.Configuration.GetSection("RabbitMq");

        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<PdfUploadedConsumer>();
            x.AddConsumer<PdfUploadedFaultConsumer>();

            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();
                o.QueryDelay = TimeSpan.FromSeconds(1);
            });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(
                    rabbit["Host"] ?? "localhost",
                    ushort.Parse(rabbit["Port"] ?? "5672"),
                    rabbit["VHost"] ?? "/",
                    h =>
                    {
                        h.Username(rabbit["Username"] ?? "guest");
                        h.Password(rabbit["Password"] ?? "guest");
                    });

                cfg.UseMessageRetry(r => r.Incremental(
                    retryLimit: 10,
                    initialInterval: TimeSpan.FromMilliseconds(500),
                    intervalIncrement: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(ctx);
            });
        });
    }
}
