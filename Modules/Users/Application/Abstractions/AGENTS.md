# Users application ports

Own the seven aggregate/repository and role-catalog interfaces. They describe
tracked lookup/write/Google identity, administrative persistence reads and role
catalog operations. Public semantic capabilities and projection models belong
to Users Contracts. Preserve default interface forwarding and cancellation.

Depend only on Users Contracts, Domain and Domain.Contracts; never on central
Application.Abstractions, Identity or an implementation. Foreign use cases must
use semantic Users capabilities rather than acquiring aggregate repositories.
