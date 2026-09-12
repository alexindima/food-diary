# Billing Domain Guidelines

- Own `BillingSubscription`, `BillingPayment`, `BillingWebhookEvent` and payment kinds. Provider names and their pure support predicate belong to Billing Domain.Contracts.
- Preserve existing CLR namespaces and EF identity.
- Keep provider SDK, EF, HTTP and secret concerns out of this project.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. BillingDomainGuard owns numeric(18,2) storage limits and three-ASCII-letter currency validation; do not add these policies to Primitives.
