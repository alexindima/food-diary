# Remove obsolete Infrastructure composition hooks

## Decision

After feature persistence moved to physical modules, central Infrastructure still
contained empty `AddFoodPersistence` and `AddModerationPersistence` methods. The
`AddFeatureRepositories` wrapper invoked those no-ops alongside the real shared
Audit and Email registrations. Repository search found no other caller or behavior.

Delete the three obsolete composition files. `AddInfrastructure` now calls
`AddAuditPersistence` and `AddEmailPersistence` directly, in the same relative
position between shared DbContext persistence and authentication registration.
Their service implementations, lifetimes and aliases are unchanged.

Audit and Email are not moved in this tranche. Audit storage is a shared technical
capability; Email outbox persistence accepts fully rendered envelopes from several
producers. Assigning either to a single feature would be a new ownership design,
not cleanup of the empty layer.

## Guardrails and compatibility

The exact composition-root assertion now lists the two shared registrations.
A separate guard rejects reintroduction of the three legacy files. Food ownership
tests inspect the real central composition root and continue to reject Products,
Recipes, RecentItems and Meals persistence registration there. Users ownership
tests inspect the same root for forbidden central repository registrations rather
than depending on a deleted placeholder file.

This changes no public API, service registration, project reference, package,
database model, migration, HTTP contract, provider or job behavior. No external
consumer compatibility decision is involved because all removed methods are
private. Verification evidence is retained under
`.artifacts/legacy-composition-evidence`.

Actual verification after removal: Infrastructure unit tests 550/550 and full
ArchitectureTests 1047/1047, with zero failures or skips. Both projects built with
zero warnings/errors from locked restores, and the isolated solution output was
cleaned successfully. The normal commit hook supplies the final full-solution
build and staged formatting check.

## Wiki observation

Wiki research found the files and existing extraction history, but classified this
private no-op cleanup as critical because generic persistence/configuration signals
dominated the bounded source evidence. The formal checks are still honored; the
classification is useful as caution but overstates the behavioral risk. No Wiki
policy, ranking or generator is changed for this task.
