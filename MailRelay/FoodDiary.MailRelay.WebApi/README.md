# FoodDiary.MailRelay

`FoodDiary.MailRelay` is FoodDiary's internal outbound email relay.

It accepts internal send requests over HTTP, persists them to PostgreSQL, writes transactional outbox records, publishes due message ids to RabbitMQ, and delivers them through a configured mail transport.

## Current Responsibilities

- `MailRelay/FoodDiary.MailRelay.WebApi`: host, config, Docker runtime, health checks, top-level composition.
- `MailRelay/FoodDiary.MailRelay.Domain`: relay domain concepts and rules as they are extracted from application/infrastructure flows.
- `MailRelay/FoodDiary.MailRelay.Presentation`: HTTP endpoints, internal request authorization, provider webhook adapters.
- `MailRelay/FoodDiary.MailRelay.Application`: relay use cases, application models, queue/delivery abstractions, processing coordination.
- `MailRelay/FoodDiary.MailRelay.Infrastructure`: PostgreSQL queue state, transactional outbox, RabbitMQ, delivery transports, DKIM, telemetry registration.

## Runtime Responsibilities

- accept authenticated internal relay requests
- persist outbound email jobs
- persist outbox messages transactionally with queued email state
- publish queued jobs to RabbitMQ
- use publisher confirms for broker publishes
- use main/retry/dead-letter RabbitMQ topology
- consume queued jobs from RabbitMQ
- retry failed deliveries by scheduling new transactional outbox records with PostgreSQL backoff
- optionally DKIM-sign outgoing mail before delivery
- deliver either through upstream SMTP submission or experimental direct-to-MX SMTP
- expose health and queue diagnostics endpoints

## Current Non-Goals

- inbox placement guarantees
- marketing-mail orchestration

## Messaging Semantics

- PostgreSQL remains the source of truth for delivery state
- `mailrelay_outbox_messages` provides transactional publish handoff to RabbitMQ
- RabbitMQ drives near-real-time processing
- failed attempts schedule the next RabbitMQ publish through the outbox at the same `available_at_utc` used by queue state
- PostgreSQL polling can remain enabled as an additional recovery path for broker disruptions

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
- `MailRelay__RequireMailgunWebhookSignature`
- `MailRelay__MailgunWebhookSigningKey`
- `MailRelay__RequireAwsSesSnsSignature`
- `MailRelayQueue__*`
- `MailRelayBroker__*`
- `MailRelayDelivery__Mode`: `SmtpSubmission` or `DirectMx`
- `RelaySmtp__*`
- `DirectMx__*`
- `MailRelayDkim__*`
- `OpenTelemetry__Otlp__Endpoint`

MailRelay should use its own PostgreSQL database. The local default is `fooddiary_mailrelay`; in Docker Compose it runs against the separate `mailrelay-postgres` service and `mailrelay-postgres-data` volume.

## Containers And Deploy

- `rabbitmq` runs as a separate infrastructure container
- `mailrelay-postgres` runs as the relay's separate PostgreSQL container
- `mailrelay-db-init` runs `MailRelay/FoodDiary.MailRelay.Initializer` to create or update the relay schema before the app starts
- `mail-relay` runs as a separate application container built from `MailRelay/FoodDiary.MailRelay.WebApi/Dockerfile`
- `api` calls `mail-relay` through `MailRelayClient__BaseUrl`
- production deploy must build and push the `mailrelay-initializer` and `mail-relay` images, run `mailrelay-db-init`, then start `rabbitmq`, `mail-relay`, and `api`

## Build And Run

```powershell
dotnet build MailRelay/FoodDiary.MailRelay.WebApi/FoodDiary.MailRelay.WebApi.csproj
dotnet run --project MailRelay/FoodDiary.MailRelay.WebApi
dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- status
dotnet run --project MailRelay/FoodDiary.MailRelay.Initializer -- update
```

For local development, keep the database connection string in WebApi user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=fooddiary_mailrelay;Username=postgres;Password=..." --project MailRelay/FoodDiary.MailRelay.WebApi
```

The project launch profile sets `Development`, so `dotnet run --project MailRelay/FoodDiary.MailRelay.WebApi` reads user secrets. If running the built `.exe` directly from `bin`, set `ASPNETCORE_ENVIRONMENT=Development` or pass the connection string through environment variables.

## Direct-To-MX

`MailRelayDelivery__Mode=DirectMx` makes the relay resolve recipient MX records and deliver directly to port 25 with opportunistic STARTTLS.

This is useful for experiments, but it is not equivalent to a production-grade mail server. Real delivery still depends on an unblocked outbound port 25, static sending IP, matching PTR/rDNS, SPF, DKIM, DMARC, and sender reputation. Consumer mailbox providers can reject or spam-folder mail from residential or low-reputation IPs even when the SMTP transaction succeeds.

For operator-facing setup guidance in Russian, see `README.ru.md`.
