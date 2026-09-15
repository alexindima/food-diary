# ADR 0047: Test project ownership and shared configuration

## Status

Accepted.

## Context

The root tests directory combines unrelated host, platform and architecture
suites with mixed Application/Domain donor tests. Every test group also imports
its build settings from that legacy directory. Folder moves must preserve test
discovery, analyzer policy, coverage, API snapshots and CI partition membership.

## Decision

Place host suites in Hosts/tests, platform composition and shared presentation
suites in Platform/tests, and architecture suites in Tooling/tests. Existing
module, service and Shared test locations remain unchanged. Place reusable
test-only helpers in Tooling/FoodDiary.Testing and prohibit production references
to that assembly.

Tooling/Testing owns TestProjects.props, test.runsettings, xunit.runner.json and
shared testing guidance. Each group imports the common settings directly through
its Directory.Build.props. The root tests folder and its forwarding import are retired after semantic
extraction of the mixed Domain donor.

The Application donor has now been semantically extracted: shared Runtime,
Application.Contracts and Email.Contracts have focused suites under Shared/tests;
module scenarios live with their owners. Actual assembly-ownership and runtime
registration boundary checks live in ArchitectureTests. Domain cases now live in 18 existing owner test projects. Shared suites must not acquire business-module dependencies, even
transitively. Modules reference the reusable assertion helper assembly instead of
linking sources from a different test suite.

Keep project and assembly identities, direct dependencies, test categories and
snapshot contents unchanged. Production host/platform paths do not move as part
of this decision. Tests are sibling projects, never nested inside production
project directories.

## Consequences

Project paths, solution groups, EditorConfig scopes, CI manifests, Wiki discovery
and documentation must move together. Architecture guards protect physical
ownership, test-support classification and the single shared configuration owner.
The existing CI partition continues to include every runnable test project once.

The mixed Domain suite was split by owner, including independent assertions in
mixed methods. Meals retains the real Recipe-to-Meal snapshot compatibility case
with an explicit test-only Recipes.Domain reference. Shared gains no business-domain
dependency; private-constructor smoke tests remain distinct from EF integration coverage.

See [test project ownership](../architecture/TEST_PROJECT_OWNERSHIP.md).
