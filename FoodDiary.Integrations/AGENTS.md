# Integrations Layer Guidelines

## Scope
Rules for `FoodDiary.Integrations/`.

## Responsibilities
- External provider adapters and SDK/HTTP clients used by the primary FoodDiary app.
- Implement abstractions declared in application-facing layers.
- Keep provider options and transport-specific registration here.
- Exception for extracted ownership: the Notifications-only web-push provider/options live in `Modules/Notifications/Infrastructure`, preserving legacy CLR namespaces. Hosts register that module explicitly.
- OpenAI, Fitbit, USDA and OpenFoodFacts providers/options now belong to their module Infrastructure/Providers folders. Hosts explicitly register them; do not restore their central registration or owner ProjectReferences. Shared HTTP bounds, URI validation and telemetry live in `Shared/FoodDiary.Integrations.Http`, which has no runtime project/package dependencies. See docs/ai/provider-adapter-ownership.md.
- Google/Telegram authentication providers and options belong to Identity; S3 image storage and options belong to Images. API and JobManager explicitly compose their provider registrations; Initializer does not acquire external-provider options. Images uses the existing internal URI validator and telemetry through one-way friend access. Do not restore central owner references, AWS/Skia/identity SDK package ownership or provider registration. See docs/ai/auth-storage-provider-ownership.md.

## Rules
- Do not add EF Core persistence or migrations here.
- Keep orchestration, quota checks, and domain workflow decisions in the application layer.
- Keep provider DTO/parsing concerns inside integration clients unless they are part of an application contract.
- Keep shared HTTP response-safety primitives in `Shared/FoodDiary.Integrations.Http/Http/`; provider request construction and response mapping remain provider-specific.
- This is the approved primary-core bridge to `FoodDiary.MailRelay.Client` and `FoodDiary.MailInbox.Client`.
- Do not reference MailRelay/MailInbox application, domain, infrastructure, presentation, or host projects.
- Keep provider configuration as typed options and avoid leaking SDK DTOs into application contracts unless intentional.

## Commands
- Build: `dotnet build FoodDiary.Integrations/FoodDiary.Integrations.csproj`
