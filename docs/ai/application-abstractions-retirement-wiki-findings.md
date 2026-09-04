# Application abstractions retirement: Wiki findings

## Outcome being evaluated

This change retires the physical central application-abstractions assembly, assigns its contracts to six narrow shared projects plus the Users module, rewires every source consumer to the direct owner, and adds architecture guardrails for the resulting dependency graph.

## Useful Wiki behavior

- `start` created a durable governed task and preserved the exact repository baseline for a change spanning shared libraries, modules, hosts, tests, Docker inputs, lockfiles and documentation.
- Repository navigation exposed the large downstream consumer set and made it clear that a coordinated rebuild is required.
- Manifest, dependency, architecture and source-impact checks correctly treat this as a repository-wide boundary change rather than a file move.
- Existing module-extraction precedents helped preserve CLR namespaces while changing physical assembly ownership.

## False positives and limitations

- Initial acceptance inferred HTTP/API, EF persistence and background-job behavioral obligations merely because hosts and infrastructure consume the contracts. Those paths are reference-graph changes, not changes to routes, schemas, jobs or delivery behavior. Applicable checks are retained; inapplicable criteria must be explicitly replanned or marked not applicable with source evidence.
- Generated module pages previously assumed every abstraction area lived below `FoodDiary.Application.Abstractions`. That assumption became false as modules acquired their own contract projects and the central assembly disappeared.
- Extraction readiness used the retired project as a generic compile probe, making the tool depend on the boundary being removed.
- Context discovery knew the former hub name but not the smaller replacement contract projects.
- The authored backend-contract review workflow still declared the deleted central `AGENTS.md` as a source. Page-contract lint caught the stale source before deeper verification; the workflow now cites the six owning shared guides.
- The catalog initially classified `FoodDiary.Application.Contracts` as a business module named `Contracts`, causing module-page generation to fail under strict property access. The catalog now excludes the shared application contract assembly alongside the existing runtime/non-business exclusions, with a regression assertion.
- Module pages counted the same public contract files twice when a manifest intentionally listed a contract root in both `abstractionAreas` and `contractProjects`. Public files are now deduplicated by repository path; the existing independent manifest/page count regression covers the fix.
- A delivery replan over the repository-wide reference and lockfile cascade remained CPU-active for more than twenty minutes after creating a 2,881-path, seven-phase, six-check manifest. It was interrupted before final publication rather than allowed to block verification indefinitely. The generated intermediate report and manifest remain in the governed task workspace for diagnosis; no successful replan is claimed.
- On the final source state, `delivery-status`, `delivery-validate -FailOnInvalid` and `delivery-critique -FailOnInvalid` likewise published only the workspace path and remained CPU-active without a gate result. Each bounded attempt was interrupted rather than represented as a pass or reject. The ordinary build, test, EF, dependency-audit and Wiki quality results below remain independently reproducible.

## Generic Wiki improvements in this change

- Module-page abstraction paths now resolve explicit manifest roots and module-local `Application/Abstractions` directories without prefixing the retired central folder.
- Extraction planning uses the same physical-path model and normalizes only a literal leading `./`; it does not strip arbitrary leading dots or slashes.
- Extraction readiness compiles against `Shared/FoodDiary.Application.Contracts`.
- Backend context discovery recognizes all six new shared contract projects.
- Tool contract tests cover the updated paths and resolver behavior.
- The frozen holdout still targeted the notification resource registration file removed by the immediately preceding resource-ownership commit. Git and current-source review show the same `AddNotificationResources` registration now belongs to `Modules/Notifications/Infrastructure/ModuleRegistration.cs`; only that expected path is relocated. The case id, query, cohort, change type, ranking, thresholds and history remain unchanged.
- The unseen corpus referenced the architecture test renamed in this change from `ApplicationAbstractionsBoundaryTests.cs` to `SharedApplicationContractsBoundaryTests.cs`. Only its expected path is updated; the case id, query, cohort, thresholds and history remain unchanged.
- The existing `notification-resource-registration-role` ranking rule still required the retired `Extensions` filename, so the correctly relocated registration ranked 21st. The rule now matches the stable semantic identity (`notification`, `resource`, `registration`) instead of the old filename shape. The engine's identity-boost branch now honors `identityScope: identity` consistently with structural-role boosts instead of silently treating it as path-only. Module inference also recognizes standard module Domain and Infrastructure roots instead of labeling them as module `Modules`. A real SQLite search regression requires the current registration in the top ten with module identity `Notifications`.

These are generic repository-model corrections; no module-specific search ranking, query, threshold or frozen evaluation target is changed.

The complete context suite also exposes an older quality gap after the frozen-100 gate: the unseen corpus reaches its overall top-ten threshold after the source-proven test-path relocation, but its `integrations-persistence` top-one cohort remains below the committed threshold. The misses concern existing MailInbox, MailRelay, Export and other persistence/provider ranking choices, not the retired application-contract assembly. They are reported as a non-green Wiki gate and are not tuned against this holdout in this task.

## Authority and remaining caveat

Current source, project files, architecture tests, accepted ADRs and the backend module manifest are authoritative. Generated `.llm-wiki` pages must be rebuilt after the combined source graph is stable. A green navigation result alone does not prove runtime coverage, and a stale generated result is not evidence that the retired project should be restored.

## Verification record

- Force-evaluate restore completed successfully. The final full `FoodDiary.slnx` build completed with zero warnings and zero errors.
- Fourteen full, unfiltered unit/application/host suites completed with 3,027 passed tests and no final failures or skips. This includes ArchitectureTests 1,160/1,160 and the central Application suite 374/374 after correcting stale assembly-ownership expectations and the literal dependency matrix.
- The full central PostgreSQL integration project completed 93/93 with no skips. The full Web API integration project completed 182/182 with no skips. Together with the preceding suites, the final test ledger contains 3,302 passed executions, zero failures and zero skips.
- EF `has-pending-model-changes` reported no model changes. The existing EF tools/runtime patch-version warning (10.0.10 versus 10.0.11) remains informational.
- The full solution NuGet audit, including transitive packages, completed successfully and reported no vulnerable packages.
- The final Wiki frozen-100 evaluation is green at 100/100 top-ten retrieval and 4/4 captured error cases. The complete Wiki facade remains non-green only because the later unseen `integrations-persistence` cohort is 6/16 top-one versus its committed threshold of 8/16. That unrelated holdout is not tuned in this task.
- Coverage collectors are intentionally not part of this boundary refactor.
