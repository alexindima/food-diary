# FoodDiary.MailRelay.Client Guidelines

## Role
Typed client package for service-to-service calls into MailRelay.

## Rules
- Keep this project independent from MailRelay application, infrastructure, presentation, and host projects.
- Put public HTTP contract DTOs under `Models/`.
- Keep DI wiring under `Extensions/`.
- Keep configuration classes under `Options/`.
- Do not add persistence, transport implementation, or MediatR dependencies here.
