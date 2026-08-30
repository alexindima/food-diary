# Billing Domain Guidelines

- Own `BillingSubscription`, `BillingPayment`, `BillingWebhookEvent`, provider names and payment kinds.
- Preserve existing CLR namespaces and EF identity.
- Keep provider SDK, EF, HTTP and secret concerns out of this project.
