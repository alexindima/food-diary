# Shared Outbox Record Contract

- Own only the dependency-free IOutboxMessage lifecycle contract shared by email, image deletion, achievements and notifications.
- Preserve the legacy namespace for source compatibility.
- No provider, EF, application orchestration or stream-specific record belongs here.
