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

`tests/FoodDiary.Application.Tests` and `tests/FoodDiary.Domain.Tests` remain mixed
donor suites. Extract their owner-specific cases after the corresponding module
moves stabilize; keep genuinely cross-module compatibility coverage explicit.
Do not relocate these suites wholesale into Shared.

## Build configuration and discovery

`Tooling/Testing/TestProjects.props` imports root `Directory.Build.props` and owns
the shared coverage exclusion, `test.runsettings` and `xunit.runner.json` copying.
Each test group imports that file directly through its `Directory.Build.props`.
The remaining root `tests/Directory.Build.props` is only a local forwarding import.

`FoodDiary.Testing` is test support, not a runnable test project or a production
dependency. Architecture discovery includes it in the test dependency matrix;
production projects must not reference it.

`scripts/ci/backend-test-groups.json` contains all runnable test projects exactly
once. Validate it with `scripts/ci/Test-BackendTestGroups.ps1`. API contract snapshots
live in `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/Snapshots`.

After a relocation, validate project/import resolution, build the solution once,
then run focused test suites without rebuilding. Preserve snapshot contents and
run real provider/HTTP scenarios when their behavior changes.
