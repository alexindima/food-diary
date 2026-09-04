# Shared application contracts

Own only cross-module application protocol: command/query markers, execution
ports, generic result mapping, pagination and temporal validation primitives.
Keep feature models, repositories, provider contracts and business rules with
their module owners. This project may depend only on generic shared libraries.
Preserve the legacy CLR namespaces while consumers migrate by coordinated build.
