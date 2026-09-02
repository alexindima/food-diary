# Export PDF infrastructure ownership

## Decision and compatibility

Follow ADR 0016's incremental adapter ownership pattern. The existing Export-owned
IDiaryPdfGenerator port has one implementation used by ExportDiaryQueryHandler.
Move its 15 rendering/image-policy source files into Modules/Export/Infrastructure
without changing their bodies (namespace alignment only). No new ADR is needed:
this applies the accepted module pattern, not a new deployment or data boundary.
The earlier application-only Export guide is updated to reflect this second step.

The new adapter references Export abstractions, Meals read contracts and MealType's
Domain owner. It does not reference central Infrastructure, EF, Resources,
Presentation or hosts. QuestPDF and Skia package declarations move with it.
Resource text is still supplied through IDiaryPdfReportTextProvider.

Web.Api, Initializer and JobManager explicitly register AddExportInfrastructure,
preserving the typed client previously supplied by AddInfrastructure; JobManager
does not acquire application handlers or schedules. The adapter supplies the
existing TimeProvider.System default with TryAdd, respecting host overrides.
All hosts require a coordinated rebuild. The internal renderer namespace moves;
no public HTTP/application contract or binary type-forwarding promise is added.

## Preserved boundaries

- Shared DbContext, migrations/snapshot and persisted data are untouched.
- PDF layout, chart generation, localization, ordering and report limits stay
  unchanged. Existing application authorization remains outside the adapter.
- Image schemes, DNS/private-IP checks and connection-time IP pinning are retained.
  Redirects/proxies remain disabled and HttpClient timeout remains five seconds.
- Per-image/report timeouts, concurrency/byte/image limits, render gate and
  cancellation behavior remain unchanged; no new logging, storage or network
  destination is introduced.
- 43 renderer Fact/Theory methods and eight PDF-only registration/network methods
  move to the module test project. Central mixed DI cases remain central.
  Existing reflection helpers move unchanged, without expanding production API.

## Verification

Actual final solution build: zero warnings/errors. Ten complete, unfiltered
VSTest suites pass with zero failures/skips: Export adapter78, Export application66,
Architecture984, central Infrastructure565, Resources22, Presentation825,
Web.Api unit247, JobManager168, Development MCP245 and HTTP integration182.
Total3382 executed cases, excluding repeated earlier runs.

The initial architecture run (981passed/3failed) exposed stale central-DI and
host-reference expectations; the complete984 rerun passed after exact updates.
Initial compiler/style failures in new tests are retained separately.
API compatibility audit reports zero structural breaking/additive/restrictions.
All15 renderer/policy files are namespace-only moves;66 existing lockfiles have
zero retained package-version changes. No EF/migration/snapshot or HTTP source
changed. Shared test settings do not enable a collector; none was invoked.
TRX, logs and audits are retained in .artifacts/export-pdf-evidence.

## Wiki observations

Start captured a clean baseline and found the Export precedent. Its initial
acceptance list describes the already-completed application extraction and
inferred persistence/migration work from central Infrastructure paths; this tranche
has no persistence change. Test-plan finds the donor PDF/DI suites but includes
unrelated provider, logging and Telegram tests. Current source and explicit
adapter ownership determine the bounded implementation.

Only proven relocated path literals in retrieval fixtures/ranking selectors are
maintained. Queries, weights, thresholds and unrelated expectations are unchanged.
Six files contain14 relocated literals; reversing those substitutions reproduces
their baseline content. Frozen holdout100 is byte-unchanged. Full MCP245 passes.
The topology query returned no PDF client despite explicit DI, and privacy
suggestions included unrelated OpenAI code plus image height as health data.
Network/lifecycle conclusions were therefore verified directly in source/tests.

Final full Wiki verification failed at the context-bundle search gate: frozen
holdout100 returned 96/100 top-1 and 98/100 top-10 (required 100/100 top-10).
Both missed targets are stale central-domain paths: DomainGuard.cs and
MealAiSession.cs. Neither target exists at the exact task base a8dac6a1 either;
the frozen corpus is unchanged by this extraction. This proves stale expectations,
not an independently measured exact-base search score or a green Wiki baseline.
No unrelated corpus/ranking repair or threshold relaxation is included.

Runtime verification is complete, but the required Wiki check and therefore
governed delivery remain blocked. A commit records the tested extraction as a
checkpoint, not full Wiki acceptance. The actual facade failure, standalone
holdout JSON, source audits and native governance outputs are retained under
.artifacts/export-pdf-evidence. Later context-bundle checks were not reached and
are not claimed passed. A separate Wiki fixture/retrieval follow-up is needed.
