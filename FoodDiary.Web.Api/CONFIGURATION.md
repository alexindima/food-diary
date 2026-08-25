# Web API Configuration

This project uses safe repository defaults plus local overrides from `dotnet user-secrets`, environment variables, or deployment secret stores.

## Tracked Files

- `appsettings.json`: safe defaults only, no real secrets
- `appsettings.Development.json`: local non-sensitive overrides
- `appsettings.Production.json`: production-safe placeholders only
- `appsettings.Template.json`: bootstrap example for local setup

## Local Setup

The project already has a `UserSecretsId`, so local secrets should normally go into user secrets instead of tracked files.

Example commands:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=fooddiary;Username=postgres;Password=your-local-password" --project FoodDiary.Web.Api
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379,abortConnect=false" --project FoodDiary.Web.Api
dotnet user-secrets set "Jwt:SecretKey" "your-32-character-or-longer-secret-key" --project FoodDiary.Web.Api
dotnet user-secrets set "OpenAi:ApiKey" "your-openai-api-key" --project FoodDiary.Web.Api
dotnet user-secrets set "TelegramAuth:BotToken" "your-telegram-bot-token" --project FoodDiary.Web.Api
dotnet user-secrets set "Email:SmtpPassword" "your-smtp-password" --project FoodDiary.Web.Api
```

You can inspect the current local secrets with:

```powershell
dotnet user-secrets list --project FoodDiary.Web.Api
```

## Production Setup

Do not commit production secrets or deployment passwords to `appsettings*.json`.

Provide sensitive values through:

- environment variables
- container/orchestrator secret injection
- cloud secret manager
- deployment pipeline secret store

## Required Sensitive Values

At minimum, real deployments should provide these outside source control:

- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:Redis` when using Redis-backed distributed cache
- `Jwt:SecretKey`
- `OpenAi:ApiKey` when AI features are enabled
- `TelegramAuth:BotToken` when Telegram auth is enabled
- `TelegramBot:ApiSecret` when bot callbacks are enabled
- `S3:AccessKeyId`
- `S3:SecretAccessKey`
- `S3:AllowPublicImageAccess` must be explicitly set to `true` when S3 is configured. The current image flow stores URLs that are reachable by users and may be shared with external AI providers; enable it only when the bucket or CDN policy intentionally permits that access.
- `Email:SmtpUser`
- `Email:SmtpPassword`

## Reverse Proxy Setup

If the API runs behind Nginx, Traefik, a cloud load balancer, or another reverse proxy, configure trusted forwarded headers explicitly.

Relevant settings:

- `ForwardedHeaders:ForwardLimit`
- `ForwardedHeaders:KnownProxies`
- `ForwardedHeaders:KnownNetworks`
- `ForwardedHeaders:AllowedHosts`
- top-level `AllowedHosts`

Defaults are intentionally conservative. When both trusted lists are empty, the host preserves ASP.NET Core's loopback-only defaults; any explicit proxy or network configuration replaces those defaults. The host does not trust arbitrary `X-Forwarded-*` values for rate limiting, request metadata, or generated hosts. `ForwardedHeaders:AllowedHosts` constrains trusted forwarded host values, while top-level `AllowedHosts` constrains the original HTTP host.

Example:

```json
"ForwardedHeaders": {
  "ForwardLimit": 1,
  "KnownProxies": [ "10.0.0.10" ],
  "KnownNetworks": [ "10.0.0.0/24" ],
  "AllowedHosts": [ "fooddiary.club" ]
},
"AllowedHosts": "fooddiary.club"
```

Only add proxies or networks that are actually under deployment control. The edge proxy must overwrite both `Host` and `X-Forwarded-Host` with the canonical public host before forwarding a request.

## Notes

- Keep repository config safe even for local examples.
- Prefer placeholders like `example.com` and `change-me`.
- If a new backend option becomes secret-bearing, document it here and keep the tracked config sanitized.
