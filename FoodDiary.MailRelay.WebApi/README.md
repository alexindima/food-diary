# FoodDiary.MailRelay

`FoodDiary.MailRelay` is FoodDiary's internal outbound email relay.

It accepts internal send requests over HTTP, persists them to PostgreSQL, writes a transactional outbox record, publishes message ids to RabbitMQ, deduplicates consumer processing through an inbox table, and delivers them through a configured mail transport.

## Current Responsibilities

- `FoodDiary.MailRelay.WebApi`: host, config, Docker runtime, health checks, top-level composition.
- `FoodDiary.MailRelay.Domain`: relay domain concepts and rules as they are extracted from application/infrastructure flows.
- `FoodDiary.MailRelay.Presentation`: HTTP endpoints, internal request authorization, provider webhook adapters.
- `FoodDiary.MailRelay.Application`: relay use cases, application models, queue/delivery abstractions, processing coordination.
- `FoodDiary.MailRelay.Infrastructure`: PostgreSQL queue state, transactional outbox, inbox deduplication, RabbitMQ, delivery transports, DKIM, telemetry registration.

## Runtime Responsibilities

- accept authenticated internal relay requests
- persist outbound email jobs
- persist outbox messages transactionally with queued email state
- publish queued jobs to RabbitMQ
- deduplicate broker deliveries through inbox tracking
- use publisher confirms for broker publishes
- use main/retry/dead-letter RabbitMQ topology
- consume queued jobs from RabbitMQ
- retry failed deliveries with backoff
- optionally DKIM-sign outgoing mail before delivery
- deliver either through upstream SMTP submission or experimental direct-to-MX SMTP
- expose health and queue diagnostics endpoints

## Current Non-Goals

- inbox placement guarantees
- bounce / complaint ingestion
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
- `MailRelayDelivery__Mode`: `SmtpSubmission` or `DirectMx`
- `RelaySmtp__*`
- `DirectMx__*`
- `MailRelayDkim__*`
- `OpenTelemetry__Otlp__Endpoint`

## Containers And Deploy

- `rabbitmq` runs as a separate infrastructure container
- `mail-relay` runs as a separate application container built from `FoodDiary.MailRelay.WebApi/Dockerfile`
- `api` depends on `mail-relay` when `EmailDelivery__Mode=Relay`
- production deploy must build and push the `mail-relay` image and start `rabbitmq`, then `mail-relay`, then `api`

## Build And Run

```powershell
dotnet build FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj
dotnet run --project FoodDiary.MailRelay.WebApi
```

## Direct-To-MX

`MailRelayDelivery__Mode=DirectMx` makes the relay resolve recipient MX records and deliver directly to port 25 with opportunistic STARTTLS.

This is useful for experiments, but it is not equivalent to a production-grade mail server. Real delivery still depends on an unblocked outbound port 25, static sending IP, matching PTR/rDNS, SPF, DKIM, DMARC, and sender reputation. Consumer mailbox providers can reject or spam-folder mail from residential or low-reputation IPs even when the SMTP transaction succeeds.

For operator-facing setup guidance in Russian, see `README.ru.md`.
