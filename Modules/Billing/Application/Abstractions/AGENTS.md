# Billing Application Abstractions Guidelines

- Own Billing repository, provider, checkout-lock, transaction and public provider-model ports.
- Preserve existing `FoodDiary.Application.Abstractions.Billing` namespaces.
- Do not reference EF Core, provider SDKs, hosts, presentation, or the central Application Abstractions project.
- Keep `IBillingMarketingConversionRecorder` in central Application Abstractions as the Billing consumer-owned cross-module port.
