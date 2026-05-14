# PdfProcessor

API + Worker для загрузки PDF и асинхронного извлечения текста. Связь через RabbitMQ, метаданные и текст в PostgreSQL.

## Стек

- .NET 8 (Minimal API + Generic Host)
- EF Core, Npgsql
- MassTransit (RabbitMQ, EF Core Outbox/Inbox)
- [PdfPig](https://github.com/UglyToad/PdfPig) для извлечения текста
- Serilog, Swagger, HealthChecks

## Запуск

```bash
docker compose up --build
```

- Swagger: http://localhost:8080/swagger
- RabbitMQ UI: http://localhost:15672 (guest/guest)
- Postgres: localhost:5432 (pdf/pdf)

## API

| Метод | Путь | Описание |
|---|---|---|
| `POST /api/documents` | multipart/form-data, поле `file` | Загрузка PDF |
| `GET /api/documents?skip=0&take=50` | | Список с пагинацией |
| `GET /api/documents/{id}` | | Метаданные + статус |
| `GET /api/documents/{id}/content` | | Извлечённый текст (`409` если не готов) |

Статус документа: `Pending` → `Processing` → `Completed`/`Failed`.

```bash
ID=$(curl -s -F "file=@sample.pdf" http://localhost:8080/api/documents | jq -r .id)
curl http://localhost:8080/api/documents/$ID/content
```

## Тесты

```bash
dotnet test
```

Интеграционный тест поднимает Postgres и RabbitMQ через Testcontainers, запускает API через `WebApplicationFactory` и Worker в том же процессе, грузит PDF и опрашивает `GET /content`. Нужен установленный Docker.

## Структура

```
src/
  PdfProcessor.Domain          сущности, enum'ы
  PdfProcessor.Contracts       сообщения шины
  PdfProcessor.Application     абстракции IFileStorage, IPdfTextExtractor
  PdfProcessor.Infrastructure  EF Core, файловое хранилище, парсер PDF
  PdfProcessor.Api             REST API
  PdfProcessor.Worker          RabbitMQ consumer

tests/
  PdfProcessor.IntegrationTests
```

Для прода: миграции вместо `EnsureCreated`, S3/MinIO вместо локальной ФС, авторизация.
