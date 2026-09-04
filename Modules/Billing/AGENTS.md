# Billing Logical Module Guidelines

## Boundary

- Own billing application workflows, application-facing ports/models, `BillingSubscription`, `BillingPayment`, `BillingWebhookEvent`, persistence mappings/adapters, and focused tests.
- Preserve the `FoodDiary.Application.Billing` assembly identity and existing CLR namespaces.
- Keep provider HTTP adapters and secrets in `FoodDiary.Integrations`; keep JobManager as scheduler-only plumbing.
- Keep the Billing-owned `IBillingMarketingConversionRecorder` consumer port in Billing Application/Abstractions; Marketing implements it through an explicit project reference.
- Keep shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Preserve webhook authenticity, provider/external-payment uniqueness, explicit transaction boundaries, retry safety, and financial identifier minimization.

## Verification

- `dotnet test Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/FoodDiary.Modules.Billing.Application.Tests.csproj`
- `dotnet test Modules/Billing/tests/FoodDiary.Modules.Billing.Domain.Tests/FoodDiary.Modules.Billing.Domain.Tests.csproj`
- `dotnet test Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests/FoodDiary.Modules.Billing.Infrastructure.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
