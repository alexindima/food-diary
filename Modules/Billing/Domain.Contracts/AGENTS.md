# Billing scalar domain contracts

Own BillingProviderNames in FoodDiary.Modules.Billing.Domain.Contracts with unchanged values.
Keep this project free of project dependencies, aggregates, persistence and provider SDKs.
Consumers reference this owner directly. Assembly moves require coordinated rebuilds.

BillingPremiumAccessPolicy is the shared pure status/time predicate; it exposes no aggregates or persistence.
