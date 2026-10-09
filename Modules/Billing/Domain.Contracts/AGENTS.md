# Billing scalar domain contracts

Own BillingProviderNames in FoodDiary.Modules.Billing.Domain.Contracts with unchanged values.
Own BillingSubscriptionId, BillingPaymentId and BillingWebhookEventId as scalar IDs.
Reference only shared Domain.Primitives for IEntityId; keep aggregates, persistence
and provider SDKs outside this project. Guid conversion into an ID is explicit.
Consumers reference this owner directly. Assembly moves require coordinated rebuilds.

BillingPremiumAccessPolicy is the shared pure status/time predicate; it exposes no aggregates or persistence.
