# Billing Domain Guidelines

- Own `BillingSubscription`, `BillingPayment`, `BillingWebhookEvent` and payment kinds. Provider names and their pure support predicate belong to Billing Domain.Contracts.
- Use namespaces matching project and folders. Preserve EF table, column, index and relationship identity.
- Keep provider SDK, EF, HTTP and secret concerns out of this project.
- Internal webhook lifecycle decisions use `BillingWebhookProcessingState`; known transitions write storage codes through the typed codec. Keep the mapped `Status` string and exact ordinal codes for persistence/projections. Unrecognized stored rows preserve their raw value and legacy transition behavior. Payment/subscription provider statuses remain open strings; do not classify them with the internal webhook enum.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. BillingDomainGuard owns numeric(19,3) storage limits and three-ASCII-letter currency validation; do not add these policies to Primitives.

Preserve provider amounts with up to three fractional digits (including Stripe BHD/JOD/KWD/OMR/TND). Never round provider money to fit persistence; all five payment monetary columns share this precision.

Internal subscription/payment/webhook identities use their distinct Billing Domain.Contracts ID types at business ports. Mapped Guid Id and nullable subscription FK remain compatible; computed TypedId/SubscriptionReference are explicitly ignored by EF. Provider identifiers stay open strings. BillingAmount and BillingCurrencyCode feed BillingMoneyObservation/BillingPaymentFinancials, which preserve amount-only, currency-only and missing observations, negative amounts, numeric(19,3) precision and existing null fallback. All owning payment mutation flows use CreateWithFinancials/ApplyProviderObservation; scalar compatibility adapters retain legacy validation and atomic mutation behavior.
