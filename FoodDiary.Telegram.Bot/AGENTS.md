# Telegram Bot Guidelines

## Scope
Rules for `FoodDiary.Telegram.Bot/`.

## Responsibilities
- Telegram transport/integration layer.
- Map bot interactions to the primary API/client-facing contract without taking direct dependencies on core backend projects.

## Rules
- Keep bot handlers thin; do not duplicate core business logic in bot command handling.
- Validate and sanitize user input at interaction boundaries.
- Keep command/callback contracts explicit and versionable.
- Do not reference `FoodDiary.Domain`, `FoodDiary.Application`, `FoodDiary.Infrastructure`, `FoodDiary.Resources`, `FoodDiary.Web.Api`, or `FoodDiary.Presentation.Api`.
- Keep bot options explicit and never commit real Telegram tokens.

## Reliability
- Handle transient Telegram/API failures with clear retry/backoff strategy.
- Keep logging and correlation context for supportability.

## Commands
- Build: `dotnet build FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj`
- Run: `dotnet run --project FoodDiary.Telegram.Bot`
- Tests: `dotnet test Hosts/tests/FoodDiary.Telegram.Bot.Tests/FoodDiary.Telegram.Bot.Tests.csproj`
- Generated FoodDiary API client: from `FoodDiary.Web.Client`, run `npm run sdk:bot:update` after API changes and `npm run sdk:bot:check:api` to verify the live contract.
- Do not hand-edit `Api/Generated/`. Frozen operations and the selected contract live in `Api/scopes.json` and `Api/bot.openapi.json`; C# templates are in `Api/Templates/` and share the frontend's pinned OpenAPI generator.
- Keep raw response ownership, response limits, expected conflicts, lease/security-version checks and domain model mapping in the bot adapters. API credentials are per request; signed storage and Telegram downloads retain their separate transport.
- `GeneratedApiClientUsageTests` in the architecture suite rejects manual HTTP dispatch, request construction and FoodDiary route literals outside generated code. Only the existing transport forwarding and signed storage PUT shapes are permitted; adding an HTTP call to the same file does not exempt it.
