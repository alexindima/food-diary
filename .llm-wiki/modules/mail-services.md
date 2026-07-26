---
id: module.mail-services
kind: module
status: current
sources:
  - docs/ARCHITECTURE.md
  - docs/BACKEND_MODULE_MAP.md
  - MailRelay/AGENTS.md
  - MailInbox/FoodDiary.MailInbox.Application/AGENTS.md
  - MailInbox/FoodDiary.MailInbox.Infrastructure/AGENTS.md
  - MailInbox/FoodDiary.MailInbox.Presentation/AGENTS.md
  - MailInbox/FoodDiary.MailInbox.WebApi/AGENTS.md
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
client packages. Current cross-service access belongs in
`FoodDiary.Integrations`; server-side service projects must not leak into the
primary backend dependency graph.

See the canonical [architecture](../../docs/ARCHITECTURE.md) and
[module map](../../docs/BACKEND_MODULE_MAP.md) before changing these boundaries.
