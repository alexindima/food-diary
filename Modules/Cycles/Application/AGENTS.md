# Cycles Application Module Guidelines

## Scope

Rules for `Modules/Cycles/Application/`.

## Boundaries

- Own cycle profile, factors, symptoms, bleeding entries, fertility signals, and their use cases.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers and validators through `AddCyclesApplication`; composition roots use Infrastructure's `AddCyclesModule` facade.
- Depend on other business areas through their owner contracts, with direct ProjectReferences for used types, not foreign implementations or central umbrella exports.

- Pass the injected TimeProvider to date-dependent calculations in read and write handlers. Nutrition interval reconstruction must not reintroduce excluded confirmed episodes through bleeding logs.
