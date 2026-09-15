# Shared application contracts

Own only cross-module application protocol: command/query markers, execution
ports, generic result mapping, pagination and temporal validation primitives.
Keep feature models, repositories, provider contracts and business rules with
their module owners. This project may depend only on generic shared libraries.
Preserve the legacy CLR namespaces while consumers migrate by coordinated build.

Authentication factories are owned by Authentication.Contracts, Users.Contracts,
Admin.Contracts and Identity.Contracts. Keep only the generic validation taxonomy
here. Do not restore the Errors.Authentication facade or add reverse owner references.

IAtomicCommand opts a top-level command into handler-plus-save atomic execution. IAtomicCommandExecutor exposes only the callback, never a DbTransaction or context. Retries may repeat the handler; external side effects and nested transactions are not permitted. Existing ITransactionalCommand retains its save-after-handler policy.
