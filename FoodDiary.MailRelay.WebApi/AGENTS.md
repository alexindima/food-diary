# Mail Relay Guidelines

## Scope
Rules for `FoodDiary.MailRelay.WebApi/`.

## Role
- Dedicated outbound email relay for FoodDiary services.
- Accepts internal email-send requests over HTTP.
- Persists outbound queue state in PostgreSQL.
- Persists transactional outbox state in PostgreSQL.
- Persists consumer inbox dedup state in PostgreSQL.
- Publishes queued work to RabbitMQ for broker-driven processing.
- Delivers mail through a configured upstream SMTP transport.
- Evolves toward a production mail subsystem without coupling the main API to direct SMTP delivery.

## Architecture
- Keep this project host-focused: `Program.cs`, configuration files, Docker host assets, health endpoints, and top-level runtime wiring.
- Keep HTTP endpoints in `FoodDiary.MailRelay.Presentation`.
- Keep domain concepts in `FoodDiary.MailRelay.Domain`.
- Keep relay use cases and abstractions in `FoodDiary.MailRelay.Application`.
- Keep PostgreSQL, RabbitMQ, SMTP, direct-to-MX, DNS, DKIM, hosted workers, and DI registration in `FoodDiary.MailRelay.Infrastructure`.
- Do not move business-specific email composition into mail relay projects; templates and auth-flow semantics stay in `FoodDiary.Infrastructure` / upper layers.

## Queue And Delivery Rules
- Treat the database queue as the source of truth for relay state even when RabbitMQ is the active broker.
- Keep queue/outbox writes transactional when changing enqueue flow.
- Treat inbox records as consumer dedup state, not as business events.
- Prefer idempotent enqueue flows when upstream callers can provide a stable idempotency key.
- Keep retry, lock timeout, and batch settings configurable through typed options.
- Preserve operational visibility: queue state, failure reasons, and message identifiers should remain observable.

## Security
- Never commit real relay API keys, SMTP credentials, or production connection strings.
- Keep relay-to-caller authentication explicit and configurable.
- When adding diagnostics endpoints, avoid exposing message bodies or secrets by default.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj`
- Run: `dotnet run --project FoodDiary.MailRelay`

## Near-Term Direction
- Current broker is RabbitMQ with PostgreSQL as the delivery state store.
- Current transports are upstream SMTP submission and experimental direct-to-MX.
- Planned production-hardening areas: DKIM signing, suppression lists, bounce/complaint ingestion, metrics, and optional alternate transports.
