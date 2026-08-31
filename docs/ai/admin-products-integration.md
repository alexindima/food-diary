# Admin and Products integration

## Scope and provenance

Integration base: `4c1b1c2c31a0886cf7e01d8bba7ec00231e6b149`.
Admin checkpoint: `7bd83153cb134c8d5a6cf0ddabd8edcb481d1716`.
Products checkpoint: `bc121dd5eb9eedb894b9e61c6253c3933dc063fb`.
Both source trees were clean. Source evidence was copied into the integration
evidence directory and checked against per-file SHA256 manifests (203 Admin
files, 251 Products files); the original worktrees and evidence remain intact.

The user requested completion of the outstanding work and integration into
local master. No push, coverage collector, production access or deployment is
part of this change. The source checkpoints' failed Wiki/governance results are
historical evidence, not passing certificates for the combined state.

## Combined ownership

Both physical modules, project references, explicit persistence mappings,
host registrations and Docker project copies are retained. The solution has
272 unique project paths, all of which exist. JobManager and central DI tests
register both persistence facades. The application abstraction allowlist is
the union of the two reviewed dependency additions, not a wildcard relaxation.

Admin's independent impersonation aggregate belongs to its module. Products'
shared CLR navigation graph remains central; its focused Domain tests belong
to the module independently of the production Domain seam. Central DbContext,
migrations and snapshot remain central. See the two module ownership inventories
for details. Integration did not redesign HTTP, authorization, provider calls,
transaction behavior, retention or persistence schema.

Conflicting generated indexes were initially bootstrapped from the Admin side
solely to make a parseable intermediate tree. Native updates have since
regenerated them from the combined graph; the placeholders were not verification evidence. NuGet locks
were regenerated through force-evaluate restore, followed by locked restore.

## General Wiki repairs

- Test planning scanned central test roots but omitted `Modules/*/tests` when
  resolving declared types and idempotency-related behavior. Discovery now
  enumerates exact module-owned test roots, excludes generated build folders,
  and retains existing priority rules. The unchanged Billing selection
  assertion is the integration regression; it is not weakened.
- The integration's first Wiki start reproduced Windows command-line overflow
  with the complete path list. Change-set snapshots now share bounded positive
  Git-path batching with read-only overlays. Tests compare small and oversized
  scopes, fingerprints, reordered duplicates, Unicode/spaces, rename endpoints,
  untracked/deleted paths and errors. Magic exclusion expressions are rejected
  rather than silently split into inequivalent queries.
- Search rules used legacy physical layer prefixes. A generic path-layout
  adapter recognizes exact module Application, Application/Abstractions,
  Domain, Infrastructure and Infrastructure/Model boundaries. Existing rule
  weights, query terms, thresholds and evaluation targets are unchanged by
  this repair. Synthetic Inventory examples test equivalent selectors and
  negative boundaries, including abstraction exclusions and test paths. The
  same adapter is implemented in the Node writer/query tool and the in-process
  .NET reader. The public-reader regression reproduced five failures among
  eleven synthetic cases before the .NET repair (six negative/legacy cases
  already passed); it does not expose a new public test seam.
- Default `Any` queries now honor the existing explicit layer-intent rules,
  including their minimum-match and exclusion conditions. A singleton generic
  implementation term must not demote workflow documentation. Translated Domain
  aliases require domain/entity intent; physical paths preserve their previous
  behavior. Short alphanumeric identifiers retain direct filename affinity.
  Module-owned unit-test paths receive the corresponding legacy test identity.
  Synthetic positive and negative tests cover these boundaries in both readers.
  During this work, validation exposed two introduced ordering regressions;
  both were repaired and the validation corpus returned to its baseline result.
- The context scanner's role-override expression matched a substring inside
  ordinary compatibility-contract prose. Whole-word boundaries remove that
  false positive. The regression also verifies that actual directive phrases
  still produce quarantined findings; no trust zone or policy severity changes.
- Smoke selection and fingerprints include the new helpers. Search dependency
  fingerprints include the ranking implementation, preventing reuse after a
  helper-only change.

These are navigation/tooling changes, not evidence that application behavior
changed. Products' already-reviewed generic module-persistence policy repair
and Admin's audited selector relocation/exclusion are preserved.

The remaining evaluation corpora contained 125 references to 113 absent paths.
Every target has one unambiguous, recorded Git rename chain to an existing file
(see `admin-products-eval-path-audit.json`). Seventeen corpus files receive only
these expected/accepted path substitutions. A semantic round-trip check proves
that queries, IDs, cohorts, thresholds, historical measurements and all other
fields are unchanged. Original corpus files are archived. The 100-case holdout
has no substitutions in this integration maintenance.

## Verification ledger

Integration evidence is retained under
`.artifacts/admin-products-integration-evidence/` (ignored, machine-local).
Source archives, failed attempts and later successful runs are kept separately.

- Combined force-evaluate restore: passed.
- Combined locked restore: passed.
- Combined full solution build: passed, zero warnings and errors.
- Bounded Git/snapshot, module test-root, ranking-layout and scanner regression
  checks: passed. Unchanged Billing test-selection assertions pass.
- Combined business runtime: 22 unfiltered suites, 4,513 passed, zero failures
  and skips, including Products PostgreSQL 3, central PostgreSQL 114 and full
  HTTP/Swagger 182. The full central PostgreSQL run took 6m43s.
- Combined Architecture: 831 passed. EF: no pending model changes, with the
  existing tooling/runtime version warning retained. NuGet audit: 272 projects,
  no vulnerable entries. Migration and HTTP snapshot diffs are empty.
- Final full MCP suite: 245 passed, zero failures/skips. Earlier 220, 231 and
  239-test runs and focused failure-before/success-after runs remain historical
  evidence, not additional tests in the final total. Latest disjoint total:
  24 unfiltered suites, **5,589 passed**, zero failures/skips.
- Actual Node/.NET parity: identical case ranks, candidate paths and scores on
  the 100-case holdout and 100-case unseen corpus. Both live gates pass:
  holdout top1 98/100, top10 100/100; unseen top1 70/100, top10 94/100, MRR 0.79.
  Validation is 49/50 top1 and 50/50 top10, matching the exact base.
- All nine non-quality affected smoke groups pass: adaptive evaluation,
  UI continuation, Git paths, change policy, code graph, tool contracts,
  verification cache, facade contracts and read-only guards (333.78s aggregate).
  Context-cache and workflow-recovery regressions and Users context retrieval
  pass separately. These do not create a passing full context-bundle receipt.

## Explicit Wiki quality exception

**The full Wiki quality gate is not green.** Native verification failures are
retained, not replaced with a synthetic aggregate PASS. The integration uses
the repository's `passed-with-known-baseline-failures` evidence disposition
only for Wiki verification, with its actual nonzero process exit retained.
The final facade execution and disposition are recorded in
`wiki-verify-handoff.log` and `wiki-verify-handoff-record.json`.

A new detached worktree at the exact integration base built its own fresh graph
(7,144 files / 31,563 symbols). Its source and ranking implementation were not
modified. The comparison preserves queries, IDs, cohorts, thresholds and numeric
history; only revision-appropriate expected paths use proven Git renames where
Admin/Products files do not yet exist at the base. Original inputs, target audit,
both measured outputs and clean baseline status are retained. This is not a
comparison against an old cached score or an assumed baseline.

Final measured residuals are identical at base and integration:

| Corpus | Target case | Expected result rank at both revisions |
| --- | --- | --- |
| Generalization | `pdf-remote-image-policy` | 2 |
| Generalization | `version-controller` | 2 |
| Generalization | `measurement-system-service` | 2 |
| Generalization | `api-snapshot-guidance` | 3 |
| Security regression | `security-webpush-connect-time` | 2 |

The generalization result is 66/70 top1, 70/70 top10, MRR 0.969, below unchanged
0.95 top1 and 0.97 MRR gates. Security-regression retrieval is 9/10 top1 and
10/10 top10, below its unchanged perfect-first-result requirement. Top competing
paths also match the exact base. No newly introduced failure is waived, no
query-specific boost is added, and no threshold or frozen numeric history is
changed. `known-baseline-exception.json` and `quality-base-comparison/` contain
the exact five-case comparison. These are search-order defects, not reports of
five application security vulnerabilities.

Broader diagnostic runs and failed intermediate attempts are retained. The
probe-7 regression improved from 39/40 at the base to 40/40 after module-owned
test identity repair. The independent live holdout and unseen gates above pass;
their success does not waive generalization/security quality failures. Further
retrieval-quality work is outside this merge's accepted baseline exception.

## Governance, context and rollout

The native final evidence bundle retains all six required checks, separates
canonical policy definitions from actual executed commands, and links the one
full central PostgreSQL run to both equivalent integration check IDs. Only the
two migration/Designer and snapshot criteria are not applicable: the model is
unchanged. Final native acceptance, proof, lineage, validation and critique
results are retained in `governance-sealed/` and the final delivery logs; a
passing governance disposition is not a claim of fully green Wiki quality.

Context scanning retains its existing trust zones, severity, limits and
quarantine. Synthetic malicious fixtures and quoted directive-like phrases may
remain quarantined; they are data, not instructions. The native scanner truncates
the 400,694-character `Test-LlmWikiTools.ps1` at 200,000 characters. A separate
full-file review using the unchanged pattern definitions found the same three
matches, none after the native limit (`context-truncation-supplement.json`).
This does not change the native receipt into a full selected-bundle scan or
assert a production security audit. Deleted donor paths are not scanned as if
they still existed.

ADR 0016 already covers incremental logical-module extraction and the central
DbContext/migration boundary. Deployment requires coordinated rebuilt hosts,
not binary forwarding of old assemblies. No deployment is performed here;
rollback would use the previous complete host artifacts, with no database
rollback needed. The merge is local, with normal hooks enabled, and the source
worktrees/evidence remain available for audit.
