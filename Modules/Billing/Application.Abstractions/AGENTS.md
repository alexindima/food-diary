# Billing Application Abstractions Guidelines

- Own Billing repository, provider, checkout-lock, transaction and public provider-model ports.
- Use `FoodDiary.Modules.Billing.Application.Abstractions` namespaces matching physical folders.
- Do not reference EF Core, provider SDKs, hosts, presentation, or the central Application Abstractions project.
- Keep `IBillingMarketingConversionRecorder` in Billing Contracts as the Billing consumer-owned cross-module port.

Expose narrow read/write repository ports; register them against one scoped concrete implementation without a combined repository interface.
