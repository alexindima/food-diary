# Shared Outbox Record Contract

- Own only the dependency-free IOutboxMessage lifecycle contract shared by email, image deletion, achievements and notifications.
- Use the canonical FoodDiary.Outbox.Abstractions namespace.
- No provider, EF, application orchestration or stream-specific record belongs here.
