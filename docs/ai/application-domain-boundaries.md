# Application aggregate boundaries

Favorites and MealPlanning consume Meals scalar types through Meals.Domain.Contracts;
their former Meals.Domain references were unused. Export consumes Cycles enums
through Cycles.Domain.Contracts and current owner read capabilities. All eleven
Cycles enums exposed by read DTOs move verbatim, preserving namespaces, names,
numeric values and wire shapes. Cycles consumer Contracts no longer imports its
aggregate Domain. Cycles aggregates and all IDs remain in Domain.

Module Application projects have no direct references to foreign aggregate Domain
projects. Infrastructure composed reads and shared DbContext remain intentional
coupling; this does not claim full transitive/runtime isolation. Direct scalar
consumers reference their exact owners. Three host Dockerfiles include the new
project and source inputs. Rebuild hosts together for assembly ownership changes.
No migration, persistence behavior, API, CSV formatting, authorization or sensitive
export policy changes are intended. Revert moves and references together.

Verification covers source hashes, exact graph, enum ownership, existing Favorites,
MealPlanning, Cycles and Export tests, and HTTP/Swagger compatibility. See the
task-local .artifacts/cycles-boundaries-verification report for execution evidence.

Removing the Favorites reference also exposed an unused private helper in Products' CentralRelocated test class. It referenced MealItem and retired Product navigation fields through the transitive dependency. Only that dead helper was removed; the separate active Products test helper and all test cases remain unchanged.
