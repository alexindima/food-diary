# ADR 0051: Canonical project namespaces

Status: Accepted

## Context

Physical module extraction retained several legacy CLR namespaces through explicit
RootNamespace values, scoped IDE0130 suppressions and project NoWarn entries.
These names obscured the assembly owning a type and allowed misplaced namespaces
to survive compilation. The remaining shared consumers are rebuilt together.

## Decision

Every C# project uses its project name as the default namespace root, followed by
the source file's physical folder path. Remove explicit RootNamespace values,
including redundant overrides in repository tools. Enable IDE0130 as an error
globally and expose RootNamespace and ProjectDir to analyzers for all projects.
NamespaceConventionTests checks physical projects, including projects outside the
solution, and rejects namespace overrides and diagnostic suppressions.

Update consumers to the owning namespaces. Add direct owner references for the 26
existing shared-type dependencies that previously relied on transitive references.
Each added edge already belongs to that consumer's transitive closure; it does not
expose new assemblies or capabilities. Update the exact dependency matrix.
Outbox lifecycle contracts and replay persistence models keep their separate
assemblies; the former shared namespace does not justify coupling their consumers.
Composition readers use FoodDiary.ReadModel.Composition and their feature folder.

This supersedes temporary namespace-retention guidance in extraction decisions.
It changes CLR source names, so consumers must be rebuilt together. It does not
change HTTP contracts, database table/column mappings, transaction behavior or
module ownership. EF model metadata references are updated consistently.

## Verification

Build the solution with analyzers enabled. Run namespace, dependency, persistence
boundary and relevant runtime tests. Review changed persistence fingerprints:
namespace/import changes must not conceal SQL or tracking changes.
