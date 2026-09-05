# Shared integration transport primitives

Own only bounded HTTP content reading, URI validation and the existing integration
meter. Keep provider-neutral runtime code free of ProjectReference and PackageReference
dependencies; root build-time analyzers are separate. Preserve legacy namespaces,
metric names/tags, byte/depth/time limits, URI rules and cancellation behavior.

Module provider adapters reference this assembly directly. Do not add
Billing, MailRelay/MailInbox clients, provider SDKs, application contracts or EF.
Internal friend access is limited to the existing consumers and focused tests.
Focused helper tests live in `Shared/tests/FoodDiary.Integrations.Http.Tests`.
