# ADR 0015: Application Module Root Folder Structure

- Status: Accepted
- Date: 2026-08-28
- Owners: Application architecture
- Related: ADR 0001, ADR 0004, ADR 0006, ADR 0008, ADR 0009
- Supersedes: None

## Context

Several extracted application modules retained a root folder that repeated the
module name, for example `FoodDiary.Application.Products/Products`. This added a
redundant namespace segment, made paths inconsistent with newer modules, and
made feature discovery less predictable for developers and AI tooling.

Products, Recipes, and Wearables are independently owned application modules.
Their repeated root folders did not represent a second business boundary. All
repository consumers of the affected namespaces are versioned and deployed
together, while the public HTTP contracts are owned by the presentation layer
and remain unchanged by this source-layout refactoring.

## Decision Drivers

- A project root already identifies its application module.
- Namespaces should align mechanically with paths relative to the project root.
- Commands and queries should be immediately discoverable in every single-area
  application module.
- Genuine multi-area modules must retain useful feature grouping without
  introducing a redundant project-name folder.
- The convention must be enforced automatically so legacy nesting does not
  return during later extractions.

## Considered Options

1. Keep repeated module-name folders and document them as legacy exceptions.
2. Remove repeated module-name folders only from Products, Recipes, and
   Wearables without adding a repository-wide convention.
3. Establish a repository-wide root-folder convention, migrate the existing
   violations, and enforce path-to-namespace alignment.

## Decision

Adopt option 3.

- A `FoodDiary.Application.<Module>` project must not contain a root folder
  named `<Module>`.
- Single-area modules place `Commands`, `Queries`, `Common`, `Models`,
  `Mappings`, `Services`, and other purpose folders directly under the project
  root.
- A module may use feature grouping below its project root when it owns several
  distinct areas. For example, `BodyMetrics/WeightEntries` and
  `BodyMetrics/WaistEntries` remain meaningful feature groups.
- Production C# namespaces must match their paths relative to the project root.
- Products, Recipes, and Wearables move out of their repeated root folders, and
  all application, presentation, dependency-injection, and test consumers move
  to the corresponding namespaces in the same change.
- The namespace migration is an intentional source and binary compatibility
  break for internal assemblies. These assemblies are not independently
  versioned public packages, so no mixed-version rollout is supported or
  required.
- HTTP routes, request and response shapes, status codes, and serialization
  contracts are unchanged.

## Consequences

### Positive

- Module paths and namespaces become shorter and consistent with the project
  boundary already expressed by the project name.
- Developers and AI tooling can find application slices without guessing
  whether a redundant module folder exists.
- New modules and future extractions have one enforceable structural rule.
- Meaningful feature grouping remains available for modules with multiple
  owned areas.

### Negative

- The migration produces broad file-move and namespace churn in Git history.
- Any out-of-repository consumer compiled against the old internal namespaces
  would need to update and rebuild.
- Path-sensitive architecture tests and documentation indexes must be updated
  with structural migrations.

## Enforcement

- Root `AGENTS.md` defines the application module root-folder convention.
- `ApplicationModuleStructureTests` rejects repeated module-name root folders
  and production namespaces that do not match their project-relative paths.
- Existing command/query folder architecture tests continue to enforce one
  feature slice per command or query folder.
- Application, presentation, and architecture test suites compile all migrated
  consumers against the new namespaces.
- LLM Wiki generated catalog, symbol, contract, quality, and module indexes are
  refreshed with structural changes.

## Follow-up

None.
