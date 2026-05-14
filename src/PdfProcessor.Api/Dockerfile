FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PdfProcessor.sln .
COPY src/PdfProcessor.Domain/PdfProcessor.Domain.csproj src/PdfProcessor.Domain/
COPY src/PdfProcessor.Contracts/PdfProcessor.Contracts.csproj src/PdfProcessor.Contracts/
COPY src/PdfProcessor.Application/PdfProcessor.Application.csproj src/PdfProcessor.Application/
COPY src/PdfProcessor.Infrastructure/PdfProcessor.Infrastructure.csproj src/PdfProcessor.Infrastructure/
COPY src/PdfProcessor.Api/PdfProcessor.Api.csproj src/PdfProcessor.Api/
COPY src/PdfProcessor.Worker/PdfProcessor.Worker.csproj src/PdfProcessor.Worker/
RUN dotnet restore src/PdfProcessor.Api/PdfProcessor.Api.csproj
COPY . .
RUN dotnet publish src/PdfProcessor.Api/PdfProcessor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
# /data/files должен принадлежать app, иначе named volume наследует root-овые права.
RUN mkdir -p /data/files && chown -R app:app /data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "PdfProcessor.Api.dll"]
