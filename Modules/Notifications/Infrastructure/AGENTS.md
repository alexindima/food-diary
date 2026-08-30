# Notifications Infrastructure

- Own repositories, web-push outbox adapter/processor and the complete web-push provider stack.
- Preserve legacy provider/persistence CLR namespaces, configuration keys and delivery behavior.
- Register persistence and provider components explicitly from executable composition roots. Initializer registers persistence only.
- Reuse central DbContext and generic outbox engine. No module migration host, queue engine or direct SMTP adapter.
- Never log endpoint/key/private payload values or weaken endpoint network validation.
