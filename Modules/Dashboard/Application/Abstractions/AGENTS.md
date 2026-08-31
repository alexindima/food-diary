# Dashboard projection ports

Own the snapshot/body/meals ports and their projection/section models. Keep legacy
CLR namespaces. Reference Contracts, UserId and Results only; no EF, HTTP, Application
implementation or central Abstractions dependency. Application must exclude this
nested project source from its own compilation.
