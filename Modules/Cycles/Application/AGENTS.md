# Cycles Application Module Guidelines

## Scope

Rules for `Modules/Cycles/Application/`.

## Boundaries

- Own cycle profile, factors, symptoms, bleeding entries, fertility signals, and their use cases.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddCyclesModule`.
- Depend on other business areas through their owner contracts, with direct ProjectReferences for used types (including Dashboard.Contracts), not foreign implementations or central umbrella exports.
