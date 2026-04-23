# FoodDiary.MailRelay

`FoodDiary.MailRelay` is FoodDiary's internal outbound email relay.

It accepts internal send requests over HTTP, persists them to PostgreSQL, writes a transactional outbox record, publishes message ids to RabbitMQ, deduplicates consumer processing through an inbox table, and delivers them through a configured upstream SMTP transport.

## Current Responsibilities

- accept authenticated internal relay requests
- persist outbound email jobs
- persist outbox messages transactionally with queued email state
- publish queued jobs to RabbitMQ
- deduplicate broker deliveries through inbox tracking
- use publisher confirms for broker publishes
- use main/retry/dead-letter RabbitMQ topology
- consume queued jobs from RabbitMQ
- retry failed deliveries with backoff
- optionally DKIM-sign outgoing mail before SMTP submission
- expose health and queue diagnostics endpoints

## Current Non-Goals

- inbox placement guarantees
- bounce / complaint ingestion
- DKIM signing
- direct-to-MX transport
- marketing-mail orchestration

## Messaging Semantics

- PostgreSQL remains the source of truth for delivery state
- `mailrelay_outbox_messages` provides transactional publish handoff to RabbitMQ
- `mailrelay_inbox_messages` provides consumer-side deduplication
- RabbitMQ drives near-real-time processing
- PostgreSQL polling remains the recovery path for due retries and broker disruptions

## Main Endpoints

- `POST /api/email/send`
- `GET /health`
- `GET /health/ready`
- `GET /api/email/queue/stats`
- `GET /api/email/messages/{id}`
- `GET /api/email/suppressions`
- `GET /api/email/events`
- `POST /api/email/suppressions`
- `POST /api/email/events`
- `POST /api/email/providers/aws-ses/sns`
- `POST /api/email/providers/mailgun/events`
- `DELETE /api/email/suppressions/{email}`

## Configuration

- `ConnectionStrings__DefaultConnection`
- `MailRelay__RequireApiKey`
- `MailRelay__ApiKey`
- `MailRelayQueue__*`
- `MailRelayBroker__*`
- `RelaySmtp__*`
- `MailRelayDkim__*`
- `OpenTelemetry__Otlp__Endpoint`

## Containers And Deploy

- `rabbitmq` runs as a separate infrastructure container
- `mail-relay` runs as a separate application container built from `FoodDiary.MailRelay/Dockerfile`
- `api` depends on `mail-relay` when `EmailDelivery__Mode=Relay`
- production deploy must build and push the `mail-relay` image and start `rabbitmq`, then `mail-relay`, then `api`

## Build And Run

```powershell
dotnet build FoodDiary.MailRelay/FoodDiary.MailRelay.csproj
dotnet run --project FoodDiary.MailRelay
```

For operator-facing setup guidance in Russian, see [README.ru.md](C:\Users\alexi\OneDrive\Документы\GitHub\food-diary\FoodDiary.MailRelay\README.ru.md).
