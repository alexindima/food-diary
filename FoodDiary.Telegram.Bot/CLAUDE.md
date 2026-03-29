# FoodDiary.Telegram.Bot

Standalone .NET 10 Worker Service — Telegram bot for quick hydration logging and diary access via WebApp.

## Architecture

Single-file core (`TelegramBotWorker.cs`) using long polling. **No project references** to other FoodDiary assemblies — communicates with the backend entirely via HTTP.

## Bot Commands

| Command/Action | Handler | Behavior |
|---------------|---------|----------|
| `/start` | `SendStartAsync` | Checks if user is linked via API; shows WebApp button (unlinked) or quick actions keyboard (linked) |
| `/help` | `SendHelpAsync` | Usage instructions |
| Callback `water:N` | `HandleCallbackAsync` | Authenticates via API, POSTs hydration entry |
| Anything else | `SendHelpAsync` | Catch-all fallback |

## API Integration

- **Auth**: `POST /api/auth/telegram/bot/auth` with `X-Telegram-Bot-Secret` header, body `{ telegramUserId }`. Returns `{ accessToken, refreshToken }`
- **Hydration**: `POST /api/hydrations` with `Authorization: Bearer <token>`, body `{ timestampUtc, amountMl }`

## Configuration (`TelegramBotOptions`)

Bound from `TelegramBot` config section:
- `Token` — Telegram Bot API token
- `WebAppUrl` — Telegram WebApp URL (diary frontend)
- `ApiBaseUrl` — FoodDiary backend API URL
- `ApiSecret` — shared secret for bot-to-API auth (min 16 chars)

All validated on startup via `ValidateOnStart()`.

## File Structure

```
Program.cs                — Host setup, DI, options validation
TelegramBotWorker.cs      — Core bot logic (BackgroundService)
TelegramBotOptions.cs     — Configuration POCO with validators
BotInputParser.cs         — Callback data parser (water:N)
BotUriHelper.cs           — URL normalization utilities
```

## Dependencies

- `Telegram.Bot` 22.0.0 — only NuGet package
- No project references (fully decoupled, HTTP-only integration)

## Constraints

- `InternalsVisibleTo` for `FoodDiary.Telegram.Bot.Tests`
- If `Token` is blank, worker logs critical and returns immediately (graceful degradation)
