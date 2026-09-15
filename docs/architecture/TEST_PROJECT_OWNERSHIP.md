# Backend test ownership

Test projects follow the component they protect. Moving a project does not change
its assembly identity, dependencies, test categories, snapshots or CI group.

| Owner | Location |
| --- | --- |
| Module behavior | `Modules/<Module>/tests/` |
| Independent service behavior | `Services/<Service>/tests/` |
| Shared libraries | `Shared/tests/` |
| Web API, JobManager and Telegram Bot hosts | `Hosts/tests/` |
| Complete EF model, composed persistence and shared HTTP presentation | `Platform/tests/` |
| Architecture, analyzers and development MCP | `Tooling/tests/` |
| Reusable test-only helpers | `Tooling/FoodDiary.Testing/` |

Hosts and Platform are physical test groups and existing solution groups.
Production host and platform projects retain their root paths in this step.
Do not nest test projects inside a production project directory.

The mixed Domain.Tests project has been removed. Its invariant, event, atomicity,
chronology and persistence-shape cases now live in 18 existing module Domain.Tests
projects. Independent assertions in mixed methods are separated by owner. Meals
retains the real Recipe-to-Meal snapshot compatibility case and explicitly references
Recipes.Domain in its test project only. No production dependency was introduced.

The mixed Application.Tests project has been removed. Runtime pipeline and queue
tests live in `Shared/tests/FoodDiary.Application.Runtime.Tests`; generic validation,
pagination, temporal policy and error resolution tests live in
`Shared/tests/FoodDiary.Application.Contracts.Tests`; email option tests live in
`Shared/tests/FoodDiary.Email.Contracts.Tests`. These suites have no business-module
dependencies. Handler, validator, mapper, repository-default and module DI cases
live in the existing owner application test projects.

ArchitectureTests owns actual CLR assembly ownership, error catalog and runtime
registration boundary checks. Ai Application.Tests retains the real nested prompt
command transaction scenario, including rollback and post-commit delivery.
Nutrition mapper compatibility inputs are asserted independently by Products,
Meals and Recipes against the same fixed scores and grades.

`FoodDiary.Testing.Assertions.ResultAssert` is the reusable result assertion helper. Module
projects reference the helper assembly instead of linking source from another
test project. Image-access helpers used only by Users remain local to Users.

## Build configuration and discovery

`Tooling/Testing/TestProjects.props` imports root `Directory.Build.props` and owns
the shared coverage exclusion, `test.runsettings` and `xunit.runner.json` copying.
Each test group imports that file directly through its `Directory.Build.props`.
The root `tests` folder and its forwarding import are retired. Common guidance
lives in `Tooling/Testing/AGENTS.md`.

`FoodDiary.Testing` is test support, not a runnable test project or a production
dependency. Architecture discovery includes it in the test dependency matrix;
production projects must not reference it.

`scripts/ci/backend-test-groups.json` contains all runnable test projects exactly
once. Validate it with `scripts/ci/Test-BackendTestGroups.ps1`. API contract snapshots
live in `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/Snapshots`.

After a relocation, validate project/import resolution, build the solution once,
then run focused test suites without rebuilding. Preserve snapshot contents and
run real provider/HTTP scenarios when their behavior changes.
