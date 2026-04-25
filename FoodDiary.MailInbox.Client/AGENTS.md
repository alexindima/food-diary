# FoodDiary.MailInbox.Client Guidelines

## Role
Typed client package for service-to-service calls into MailInbox.

## Rules
- Keep this project independent from MailInbox application, infrastructure, presentation, and host projects.
- Put public HTTP contract DTOs under `Models/`.
- Keep DI wiring under `Extensions/`.
- Keep configuration classes under `Options/`.
- Do not add persistence, SMTP listener, or MediatR dependencies here.
