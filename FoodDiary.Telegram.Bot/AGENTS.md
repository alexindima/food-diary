# Telegram Bot Guidelines

## Scope
Rules for `FoodDiary.Telegram.Bot/`.

## Responsibilities
- Telegram transport/integration layer.
- Map bot interactions to Application use-cases.

## Rules
- Keep bot handlers thin and delegate business logic to Application layer.
- Validate and sanitize user input at interaction boundaries.
- Keep command/callback contracts explicit and versionable.

## Reliability
- Handle transient Telegram/API failures with clear retry/backoff strategy.
- Keep logging and correlation context for supportability.

## Commands
- Build: `dotnet build FoodDiary.Telegram.Bot/FoodDiary.Telegram.Bot.csproj`
- Run: `dotnet run --project FoodDiary.Telegram.Bot`
