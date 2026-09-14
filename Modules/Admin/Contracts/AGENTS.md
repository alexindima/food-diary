# Admin consumer contracts

Own the existing ExchangeAdminImpersonationCommand consumed by Identity Presentation. Preserve signatures; namespaces follow the project name and physical folders. Handlers, validators, authorization and persistence remain with their existing owners. This project exports no aggregate or repository capability. Rebuild consumers together after assembly relocation.

SendBugAcknowledgementsCommand is the background mail workflow entrypoint. It uses IRequest<Unit> without an automatic unit-of-work save; email dispatch and receipt commits remain separately controlled by the handler and receipt adapter.
