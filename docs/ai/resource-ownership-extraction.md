# Module-owned backend resources

## Decision

The central `FoodDiary.Resources` assembly is retired. Its two independent adapters now live with the modules whose application contracts they implement:

- Notifications owns `NotificationResourceRenderer` and the neutral/Russian notification templates under `Modules/Notifications/Infrastructure/Resources`.
- Export owns `DiaryPdfReportResourceTextProvider` and the neutral/Russian PDF report text under `Modules/Export/Infrastructure/Resources`.

No shared resource project replaces the retired assembly. The resources have different owners and are not reusable primitives. Interfaces remain in the existing module Application/Abstractions projects so application workflows continue to depend on semantic text contracts rather than `ResourceManager`.

API composes both adapters through `AddNotificationResources` and `AddExportResources`; JobManager composes notification text explicitly. Both registrations preserve the previous singleton lifetime. The resource owner projects declare English as the neutral language.

## Compatibility evidence

The exact baseline is `c5fa438cde2cd1d84d11c7c383b2b06c7bb27cd6`. The four `.resx` files are content-identical after relocation. The two providers and two focused test files are source-equivalent after reversing only their declared namespace and resource base-name changes. Resource keys, format placeholder indexes, localized text, culture fallback, HTTP contracts, schedules and persistence are unchanged.

Focused resource contract tests now verify:

- identical neutral/Russian keys and format placeholders;
- complete notification-type and PDF-text-contract coverage;
- `en` neutral-language metadata and loadable `ru` satellite assemblies;
- exact manifest resource names and singleton registration identity;
- representative Russian rendering through each application-facing interface.

Release publish output for API and JobManager contains both module-owned Russian satellite assemblies and no retired `FoodDiary.Resources` assembly. This is a coordinated host rebuild: the provider CLR namespaces and satellite assembly names intentionally follow their physical module owners.

## Verification scope

The final focused and consumer verification used repository-level artifacts output and no coverage collector. The final, non-overlapping test executions total 3,244 passed, 0 failed and 0 skipped:

- Export Infrastructure 88 and Application 66;
- Notifications Infrastructure 93 and Application 111;
- central Infrastructure unit tests 308;
- JobManager 168, Web API unit tests 247 and Presentation 825;
- the complete unfiltered Web API integration suite 182;
- ArchitectureTests 1,156.

The full solution build passed with zero warnings and zero errors. The NuGet transitive vulnerability audit found no vulnerable packages. Architecture health passed with 1,040 production edges and 776 test edges and no enforced drift. The first ArchitectureTests execution exposed only a stale generated repository catalog entry for the retired project; after the required Wiki update, the complete suite passed 1,156/1,156.

API and JobManager publish inspection found `ru/FoodDiary.Modules.Export.Infrastructure.resources.dll` and `ru/FoodDiary.Modules.Notifications.Infrastructure.resources.dll`, and found neither `FoodDiary.Resources.dll` nor its retired satellite assembly.

EF mappings, migrations and the model snapshot are outside this change. No provider network call, deployment or production access is involved.

## Wiki observations

Wiki research found both providers and the relevant focused tests, which was useful for confirming the split. Its generated catalog initially remained stale until the required update, causing the repository-catalog architecture guard to fail exactly on the retired project. The frozen retrieval corpus contained two stale provider paths; only those source-proven paths and the corresponding localized-report ranking prefix were relocated. Queries, expected behavior, weights, thresholds and history are unchanged.

The historical `direct-contract-reference-inventory.json` remains untouched because it records the project graph of its explicitly declared earlier baseline rather than current ownership.

The affected update completed all 12 generators and source-impact review covered all 54 affected pages. The standalone failure-knowledge, change-policy and source-impact stages passed; change policy reported 63 paths, seven rules and zero violations.

The full Wiki facade is deliberately not reported as green. It exited 1 at the frozen 100-query retrieval gate with top-10 accuracy 99/100 and error-capture rate 0.8. The exact task base already records an independent clean-base measurement with the same top-10 result and the same sole `DomainGuard` miss (`docs/ai/provider-adapter-ownership.md`). This change did not weaken a query, expected target, ranking weight or threshold. Governance therefore records only this check as `passed-with-known-baseline-failures`; it does not certify the facade stages after the throwing gate.

With that measured exception explicit, the delivery validation passed all six gates: 14/14 acceptance criteria satisfied, 63 changed paths with no unplanned or out-of-scope paths, no proof findings, unresolved reviews or lineage issues. The final delivery critique approved the change at 100/100. The task context assessment was valid with no findings or quarantined fragments.
