FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PdfProcessor.sln .
COPY src/PdfProcessor.Domain/PdfProcessor.Domain.csproj src/PdfProcessor.Domain/
COPY src/PdfProcessor.Contracts/PdfProcessor.Contracts.csproj src/PdfProcessor.Contracts/
COPY src/PdfProcessor.Application/PdfProcessor.Application.csproj src/PdfProcessor.Application/
COPY src/PdfProcessor.Infrastructure/PdfProcessor.Infrastructure.csproj src/PdfProcessor.Infrastructure/
COPY src/PdfProcessor.Api/PdfProcessor.Api.csproj src/PdfProcessor.Api/
COPY src/PdfProcessor.Worker/PdfProcessor.Worker.csproj src/PdfProcessor.Worker/
RUN dotnet restore src/PdfProcessor.Worker/PdfProcessor.Worker.csproj
COPY . .
RUN dotnet publish src/PdfProcessor.Worker/PdfProcessor.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data/files && chown -R app:app /data
USER app
ENTRYPOINT ["dotnet", "PdfProcessor.Worker.dll"]
