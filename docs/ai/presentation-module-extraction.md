# Presentation module extraction

## Scope

The primary HTTP adapter was split in one coordinated change. Thirty-two business modules now own a `Presentation` project and thirty-nine feature folders moved out of `FoodDiary.Presentation.Api`. `DailyAdvices` and `RecentItems` did not receive empty Presentation projects because they expose no dedicated controller surface.

The central `FoodDiary.Presentation.Api` project is now a shared transport kernel. It retains base controllers, current-user binders, shared filters and security attributes, common error/result mapping, SignalR hubs and the version endpoint. `FoodDiary.Web.Api` remains the executable composition root and explicitly references and registers every module Presentation assembly.

## Ownership decisions

- Feature controllers, HTTP requests, responses and mappings belong to `Modules/<Feature>/Presentation/Features`.
- Module Presentation projects reference the shared kernel and their own Application, contracts and Domain types used in the HTTP schema.
- Cross-module Presentation references are explicit only where an existing composite response reuses another module's HTTP shape: Dashboard, Dietologist, Meals, Products, Recipes, Statistics, BodyMetrics, Identity and Users.
- Admin also maps the existing Fasting telemetry and Marketing attribution application models. Identity maps the existing Admin impersonation exchange command.
- The two Dietologist relationship response records used by both Dietologist and Users live in the dependency-free `FoodDiary.Modules.Dietologist.Presentation.Contracts` project. This removes a `Users Presentation -> Dietologist Presentation -> Users Presentation` assembly cycle while preserving the original CLR names, JSON payload and OpenAPI component names.
- Billing webhook, Fasting client-telemetry and Identity authentication-cookie processors/filters moved with their owning HTTP surfaces and are registered by their module Presentation facades.
- HTTP route templates, API versions, authorization attributes, request limits, payload names and Swagger-visible record shapes were otherwise preserved.

## Guardrails

Architecture tests now:

- discover controller feature folders across the central kernel and every module Presentation project;
- require exactly thirty-two module Presentation projects and an explicit Web API reference and registration for each;
- reject Infrastructure and host references from module Presentation projects;
- reject unapproved cross-module Application/Domain references;
- apply request/response placement, controller dependency, claims, result mapping and time-provider conventions across all Presentation roots;
- keep project references grouped and require Docker restore/source copies for every transitive Presentation project.

The exact dependency matrix includes every module Presentation project and its direct references. A single explicit list supplies the same thirty-two module edges to the Web API and the three shared HTTP test projects, while the stronger role/ownership guard independently rejects forbidden dependency kinds.

## Wiki findings and improvement

Wiki `start` correctly refused to overwrite an unrelated active governed workspace, so this task uses `.artifacts/llm-wiki/tasks/presentation-extraction`. Initial navigation still treated `FoodDiary.Presentation.Api/Features/*` as the feature owner and its generated repository catalog did not know about the new projects until regeneration.

The module-page generator previously added only the legacy central feature path. It now derives `Modules/<Feature>/Presentation` from each module's `logicalRoot`, retaining the legacy fallback for incremental migrations. The backend module manifest and source-proven evaluation fixture paths were updated from their old locations without changing eval queries, weights, cohorts, thresholds or accepted behavior. A regression assertion protects module Presentation discovery.

Architecture-health generation previously treated the Web API's explicit references to module Presentation projects as undeclared drift because its lightweight parser cannot expand the shared exact project list used by architecture tests. It now recognizes only the generic `FoodDiary.Web.Api -> FoodDiary.Modules.*.Presentation` composition edge; module-to-module and other host edges remain governed by parsed exact rows. The final health check reports 1,239 production edges, 930 test edges and no enforced drift.

The API-compatibility tool previously compared HTTP DTOs by their old file paths and reported 269 false removals after the physical move. It now follows staged Git renames and pairs an unstaged delete/add only when contents match exactly, while continuing to report edited or genuinely removed DTOs conservatively. Its focused regression and the repository comparison pass with zero structural breaking, additive or behavioral changes.

The full Wiki verify is intentionally not reported as green. Its actual remaining failure is the frozen context-search holdout at top-10 99/100 (required 100/100); after one source-proven baseline path correction, the same case still ranks outside the top ten. Search ranking, queries, thresholds and accepted targets were not tuned against the frozen holdout. All source-impact reviews are current, and the targeted module-model, API-compatibility and architecture-health regressions pass. Governed delivery therefore has all nine acceptance criteria satisfied and proven, 633 paths in scope, zero plan/proof/review/lineage issues, but remains blocked by the single `wiki-verify` check; the final critique rejects the delivery certificate for that explicit reason and for an unassessed selected AI context.

Generated Wiki content remains navigation rather than authority; source, project references, HTTP integration snapshots and architecture tests are the final evidence.

## Compatibility and rollout

There is no database or migration change. Deployment requires a coordinated rebuild of the Web API because controllers now live in additional assemblies. The host registers every assembly through `Add<Feature>Presentation`; running an old host with the new module binaries is not a supported mixed-version configuration.

No coverage collector, external provider call, deployment or production access is part of this change.

## Verification

- Full solution build: 0 warnings and 0 errors.
- Presentation contracts: 825/825 passed.
- Web API unit tests: 247/247 passed.
- Architecture tests: 1,175/1,175 passed.
- Full unfiltered Web API integration and Swagger contracts: 182/182 passed.
- Total unique final test executions: 2,429 passed, 0 failed, 0 skipped.
- EF pending-model check: no model changes.
- NuGet vulnerability audit: 363 projects, 0 vulnerable packages.
- API compatibility: 0 structural breaking, 0 additive and 0 behavioral restrictions.
- Wiki source-impact review: 60 affected pages current and reviewed; full Wiki quality remains non-green as documented above.
