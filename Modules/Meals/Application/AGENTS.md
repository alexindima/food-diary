# Meals Application

Own meal diary commands, queries, validation, mappings and orchestration. Keep
owner-only aggregate repository ports under `Application.Abstractions`; expose
foreign-module reads only through `Contracts`. Register handlers through
`AddMealsApplication`; complete runtime composition belongs to module Infrastructure.

CreateMealCommand and RepeatMealCommand are top-level IAtomicCommand requests: their owner writes and Gamification evaluation enqueue commit together. Recognition creation invokes the local handler inside its existing serialized transaction; do not dispatch another top-level atomic command from that transaction.
