# Notifications Infrastructure

- Own repositories, web-push outbox adapter/processor and the complete web-push provider stack.
- Own notification resource rendering and neutral/Russian templates. Preserve keys, format placeholders, encoding, culture fallback and singleton registration through AddNotificationResources.
- Preserve legacy provider/persistence CLR namespaces, configuration keys and delivery behavior.
- Register persistence and provider components explicitly from executable composition roots. Initializer registers persistence only.
- Reuse central DbContext and generic outbox engine. No module migration host, queue engine or direct SMTP adapter.
- Never log endpoint/key/private payload values or weaken endpoint network validation.
- Use the injected `TimeProvider` for delivery deadlines as well as payload timestamps. Preserve caller cancellation separately from delivery timeout; deadline tests should advance a controlled timer after delivery starts.
- Own WebPushOutboxReplayStream queries and notification-id preview metadata; AddNotificationsPersistence registers the scoped shared-engine extension. The central coordinator alone owns replay transaction, audit and saving. Preserve FOR UPDATE, stream order/name and shared context identity.
