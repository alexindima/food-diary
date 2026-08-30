# Dietologist Wiki-first extraction report

## Outcome

Dietologist was extracted into Application, Application Abstractions, Domain, PersistenceModel, and Infrastructure projects under `Modules/Dietologist`, plus module-owned Application, Domain, and Infrastructure test projects. Presentation, hosts, shared DbContext, migrations, snapshot, HTTP tests, architecture tests, and true cross-module tests remain central.

## What Wiki found correctly

- The module page identified Dietologist as an aggregate owner and highlighted the consumer-owned batch attention projection.
- `privacy` found weight, calorie, waist, hydration, and fasting sharing fields and correctly made permission preservation a design constraint.
- `research` found relevant extraction precedents and required a design checkpoint.
- `ownership`, `decision`, and the generated catalog were useful cross-checks against current source and solution membership.
- `update` transactionally regenerated source and derived indexes.

## What Wiki missed or distorted

- `research` reported only eight grounded paths and incorrectly centered its delta on twelve `IUserContextService` consumers, missing most Dietologist aggregates, ports, EF configurations, repositories, hosts, and tests.
- `delivery-status` inferred five background-job acceptance criteria (registration, configuration, cancellation, retry safety, and direct consumers) even though the extraction neither introduced nor changed a Dietologist background job. Those criteria were left unresolved instead of manufacturing evidence for an unrelated concern.
- `brief -Query` and `test-plan -Query` returned zero risk/files/commands because planned paths were not carried through the facade invocation.
- `decision` did not trigger an ADR even though project references, DI ownership, EF model registration, and solution topology changed.
- The pre-update backend module manifest had an empty Dietologist mapping despite current source evidence.
- The JSON fallback warned that TypeScript prerequisites were unavailable and was materially less complete than source/project-graph inspection.

## Commands that helped

`start`, `research`, `design`, `ownership`, `decision`, `privacy`, `update`, and `verify` provided governance, privacy evidence, precedents, and deterministic refresh. Direct `rg`, project inspection, architecture tests, and builds remained necessary authority.

## Recommendations

1. Carry `start` planned paths into later `brief` and `test-plan` calls.
2. Rank feature-named projects and matching EF/repository paths above generic interface matches.
3. Trigger `decision` for csproj, solution, DI, or persistence-model registration changes.
4. Make fallback confidence list omitted graph capabilities and the compiled-index prerequisite.
5. Add an extraction inventory grouped by Application, ports, Domain, persistence model, adapters, hosts, tests, and foreign consumers.
