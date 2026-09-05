# Remaining backend boundaries batch

## Scope

This batch closes four related ownership gaps without changing HTTP, provider, database, or domain behavior:

1. delete the legacy `FoodDiary.Integrations` umbrella project;
2. relocate clearly owner-specific tests from central test assemblies;
3. move Identity and Notifications presentation adapters beside their feature presentation layers;
4. narrow eligible module repositories from the shared `FoodDiaryDbContext` to reviewed owner `DbSet<T>` capabilities.

## Adapter ownership

- Billing owns its Paddle, Stripe and YooKassa gateways, options, validation and public provider configuration under `Modules/Billing/Infrastructure/Providers`.
- Admin owns the MailInbox reader bridge under `Modules/Admin/Infrastructure/Integrations/MailInbox`.
- MailRelay-backed email delivery is a narrow shared adapter in `Shared/FoodDiary.Email.MailRelay` because email delivery is consumed by multiple modules and is not Billing/Admin ownership.
- Provider-neutral bounded HTTP reading, URI validation and telemetry remain in `Shared/FoodDiary.Integrations.Http`.
- Hosts compose these owners explicitly; there is no replacement umbrella `AddIntegrations` production API. The identically named helper in `FoodDiary.Infrastructure.Tests` is test-only composition for existing mixed registration cases.

The physical deletion preserves legacy CLR namespaces where changing them would create unrelated public/internal churn. Project ownership, references and host composition—not namespace spelling—are authoritative.

## Presentation ownership

Identity Presentation now owns email-verification SignalR, refresh-token cookie handling and Telegram bot-secret authorization. Notifications Presentation owns its hub and notification pusher. `FoodDiary.Presentation.Api` retains reusable controller, mapping, filtering, telemetry and SignalR user-identity primitives only. API endpoint mapping explicitly invokes both module presentation mappings.

## Test ownership

Focused Admin, Identity, Products and Recipes application tests moved to their existing module test projects. The mixed dashboard validator file was split between DailyAdvices and Statistics. Billing/Admin provider tests, Identity/Notifications presentation tests, MailRelay transport tests and bounded HTTP helper tests moved to their physical owners. Cross-module persistence, outbox, DbContext and HTTP contract suites stay central because their subject is the shared transaction/model/host boundary rather than a single module.

## Shared context decision

ADR 0029 remains in force: one scoped `FoodDiaryDbContext`, one unit of work and one migration history are retained. This batch does not introduce multiple contexts or a generic context facade.

The simple Billing payment/subscription/webhook, Identity email-template, and Notifications notification/subscription repositories now accept only their owner `DbSet<T>`. Their module registrations resolve those sets from the shared scoped context. Transaction runners, outbox processors, raw SQL adapters, ChangeTracker users and cross-module projections retain the full context because narrowing them would remove required atomicity or query capability.

`PersistenceCapabilityTests` and `docs/architecture/persistence-capabilities.json` record and enforce both the narrowed constructors and composition-time set acquisition.

## Compatibility and rollout

- No route, payload, Swagger snapshot, database mapping, migration, provider request, retry, timeout, credential, or persistence behavior intentionally changes.
- All executable hosts must be rebuilt together because the deleted project is a physical assembly boundary change.
- The shared DbContext, migrations and model snapshot remain central, so no data migration or rollback operation is required.
- Provider configuration keys and legacy implementation namespaces are preserved.

## Verification record

Final build, focused suites, architecture guardrails, PostgreSQL/HTTP integration, EF pending-model check, NuGet audit and Wiki results are recorded in the task handoff after execution. Coverage collectors are intentionally not used.
