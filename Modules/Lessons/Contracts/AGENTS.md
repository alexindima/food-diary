# Lessons consumer contracts

Expose administration commands/queries and immutable results through the shared Mediator. Consume Lessons scalar
types through Domain.Contracts, never the aggregate-bearing Domain assembly.
Preserve defaults, cancellation, publication flags, import deduplication and result semantics. Mutating owner requests stage changes in the caller unit of work; they do not save independently.

Use canonical FoodDiary.Modules.Lessons project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
