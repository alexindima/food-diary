---
id: module.mail-services
kind: module
status: current
sources:
  - docs/ARCHITECTURE.md
  - docs/BACKEND_MODULE_MAP.md
  - Services/MailRelay/AGENTS.md
  - Services/MailInbox/FoodDiary.MailInbox.Application/AGENTS.md
  - Services/MailInbox/FoodDiary.MailInbox.Infrastructure/AGENTS.md
  - Services/MailInbox/FoodDiary.MailInbox.Presentation/AGENTS.md
  - Services/MailInbox/FoodDiary.MailInbox.WebApi/AGENTS.md
  - Services/BugTriage/AGENTS.md
  - docs/backend/BUG_TRIAGE.md
  - docs/backend/OUTGOING_MAIL.md
  - docs/adr/0036-independent-bug-triage-service.md
---

# Mail Services

MailRelay and MailInbox are supporting bounded contexts with their own hosts,
databases, layers, and client packages.

## MailRelay

MailRelay owns outbound mail delivery, including persistence, queueing,
RabbitMQ, SMTP/direct-to-MX behavior, DNS, DKIM, and workers. Its Web API
project is a composition root; HTTP contracts live in presentation.

## MailInbox

MailInbox owns inbound SMTP and MIME processing. Its infrastructure layer owns
runtime listeners and persistence, presentation owns HTTP transport, and its Web
API remains a host-only project.

## Core Integration Boundary

Primary FoodDiary projects interact with these services only through their
client packages. Admin Infrastructure owns the MailInbox bridge and
`Shared/FoodDiary.Email.MailRelay` owns outbound transport; server-side service
projects must not leak into the primary backend dependency graph.

The same shared MailRelay adapter exposes the outgoing journal to Admin through
email contracts. Admin owns the optional bug acknowledgement poller and opaque
MailInbox receipt IDs; Identity retains editable template ownership. Auto-reply
headers and purpose metadata survive the relay queue. See the
[outgoing-mail runbook](../../docs/backend/OUTGOING_MAIL.md) for privacy and opt-in
activation; this does not change BugTriage's report lifecycle.

## BugTriage Consumer

BugTriage is an independent operational service with a separate database. Its
Infrastructure consumes MailInbox.Client's recipient-filtered export and binary
MIME routes. It owns report leases and outcomes; local Codex tasks own code
investigation and draft publication. MailInbox remains generic and has no
BugTriage dependency. See [the runbook](../../docs/backend/BUG_TRIAGE.md).

See the canonical [architecture](../../docs/ARCHITECTURE.md) and
[module map](../../docs/BACKEND_MODULE_MAP.md) before changing these boundaries.
