# Mail Relay Guidelines

## Scope
Rules for `FoodDiary.MailRelay/`.

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
- Keep this project host-focused: HTTP endpoints, queue orchestration, delivery worker, runtime diagnostics.
- Do not move business-specific email composition into this project; templates and auth-flow semantics stay in `FoodDiary.Infrastructure` / upper layers.
- Keep delivery mechanics isolated behind relay services so future transports such as direct-to-MX or provider APIs can be added cleanly.

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
- Build: `dotnet build FoodDiary.MailRelay/FoodDiary.MailRelay.csproj`
- Run: `dotnet run --project FoodDiary.MailRelay`

## Near-Term Direction
- Current broker is RabbitMQ with PostgreSQL as the delivery state store.
- Current transport is upstream SMTP.
- Planned production-hardening areas: DKIM signing, suppression lists, bounce/complaint ingestion, metrics, and optional alternate transports.
