# Billing Application Abstractions Guidelines

- Own Billing repository, provider, checkout-lock, transaction and public provider-model ports.
- Use `FoodDiary.Modules.Billing.Application.Abstractions` namespaces matching physical folders.
- Do not reference EF Core, provider SDKs, hosts, presentation, or the central Application Abstractions project.
- Dispatch Marketing.Contracts RecordPremiumConversionCommand for conversion recording. Do not restore a Billing-owned conversion service port; the caller retains the unit of work.

Subscription reads use IBillingSubscriptionReadModelRepository projections; tracked aggregate lookups belong only to IBillingSubscriptionWriteRepository. Expose narrow read/write repository ports; register them against one scoped concrete implementation without a combined repository interface.
