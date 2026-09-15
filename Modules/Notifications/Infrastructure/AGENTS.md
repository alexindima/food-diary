# Notifications Infrastructure

- Own repositories, web-push outbox adapter/processor and the complete web-push provider stack.
- Own notification resource rendering and neutral/Russian templates. Preserve keys, format placeholders, encoding, culture fallback and singleton registration through AddNotificationResources.
- Preserve legacy provider/persistence CLR namespaces, configuration keys and delivery behavior.
- Register persistence and provider components explicitly from executable composition roots. Initializer registers persistence only.
- Use the owner context and shared Outbox.Infrastructure engine. No module migration host, queue engine or direct SMTP adapter.
- Never log endpoint/key/private payload values or weaken endpoint network validation.
- Use the injected `TimeProvider` for delivery deadlines as well as payload timestamps. Preserve caller cancellation separately from delivery timeout; deadline tests should advance a controlled timer after delivery starts.
- Own WebPushOutboxReplayStream queries and notification-id preview metadata; AddNotificationsPersistence registers the scoped shared-engine extension. The central coordinator alone owns replay transaction, audit and saving. Preserve FOR UPDATE, stream order/name and shared connection identity.

Runtime writes and normal outbox processing use the owner context registered through
CreateModuleContext. Processors retain a shared-scope clean-entry callback before
claiming. Replay streams use their owner context on the shared connection. The central
coordinator resets all registered trackers and saves audit plus owner changes through IUnitOfWork. User purge and image ownership reassignment retain their shared
transaction scope; they are not ordinary runtime repository dependencies.

Resolve IModuleScopeGuard for the processor's clean-entry callback instead of the concrete shared context. Invoke the live check before each claim; the shared engine and owner claimer retain their existing lifecycle and local transaction checks.

Shared outbox claiming, processing, policy, options and telemetry now belong to `Shared/FoodDiary.Outbox.Infrastructure` (see its AGENTS.md). Images, Notifications and Gamification Infrastructure reference that narrow runtime, never central Infrastructure, including transitively. Central Infrastructure retains replay coordination and the email adapter. The runtime checks `IModuleScopeGuard` on coordinated contexts; owner callbacks and dedicated-context clean-entry checks remain in force.
