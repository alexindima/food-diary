# Recipes read contracts

Own RecipeErrors with unchanged error codes, messages and kinds; consumers must
not reference internal Abstractions merely for error factories.

Own existing access, lookup, overview services and projection/filter records. Use canonical folder namespaces and preserve signatures. Return projections, never foreign aggregate mutation rights. Central Domain IDs/enums remain compatibility types. No handlers, EF, HTTP or providers.

Current module convention: all projects use `FoodDiary.Modules.Recipes.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
