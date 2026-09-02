# Users Domain Contracts

Own only UserId with its existing CLR namespace and conversion semantics.
The only project dependency is shared Domain.Primitives for IEntityId.
ID-only consumers use this seam without acquiring the User aggregate.
