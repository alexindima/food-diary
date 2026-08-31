# Meals Application

Own meal diary commands, queries, validation, mappings and orchestration. Keep
owner-only aggregate repository ports under `Application/Abstractions`; expose
foreign-module reads only through `Contracts`. Register handlers through
`AddMealsApplication`; complete runtime composition belongs to module Infrastructure.
