using PdfProcessor.Worker;
using HostingHost = Microsoft.Extensions.Hosting.Host;

var builder = HostingHost.CreateApplicationBuilder(args);

WorkerHostBuilder.Configure(builder);

var host = builder.Build();
await host.RunAsync();
