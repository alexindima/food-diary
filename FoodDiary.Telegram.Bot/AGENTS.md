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
- Tests: `dotnet test tests/FoodDiary.Telegram.Bot.Tests/FoodDiary.Telegram.Bot.Tests.csproj`
