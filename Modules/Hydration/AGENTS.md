# Hydration Logical Module Guidelines

## Scope

Rules for `Modules/Hydration/`.

## Boundaries

- Own hydration entries, daily totals, hydration goals, and their use cases.
- Keep the real application assembly at `Application/FoodDiary.Modules.Hydration.Application.csproj`; do not recreate a root module project or an empty wrapper.
- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddHydrationApplication`; composition roots use Infrastructure's `AddHydrationModule` facade.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
- Keep the shared `FoodDiaryDbContext`, migrations, and model snapshot in central Infrastructure.
- Keep `HydrationEntry`, `HydrationEntryId`, and the public `User.HydrationEntries` navigation in central Domain until the `User` aggregate boundary can be changed without a CLR or EF compatibility break.
- Preserve legacy `FoodDiary.Application.Hydration.*` and Hydration domain CLR namespaces during this extraction.
