# Supplemental Dashboard fixture evaluations

Numerical baseline **UNKNOWN** for these current-worktree results. They are not covered by the frozen-100 exception. CLI success does not imply semantic JSON success. No wider baseline replay or repair was performed.

| Corpus | Cases | Top1 | Top10 | Exit | JSON passed | All violated thresholds / switch or live gates |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `context-search-generalization` | 70 | 63/70 | 66/70 | 0 | false | top1Rate=0.9<0.95; top10Rate=0.9429<1.0; meanReciprocalRank=0.9214<0.97; top1=0.9<0.95; top10=0.9429<1; mrr=0.9214<0.97 |
| `context-search-holdout` | 40 | 30/40 | 31/40 | 0 | false | top1Rate=0.75<0.85; top10Rate=0.775<0.95; meanReciprocalRank=0.7625<0.85; top1=0.75<0.95; top10=0.775<1; mrr=0.7625<0.97 |
| `context-search-probe-3` | 30 | 26/30 | 26/30 | 0 | false | top1Rate=0.8667<0.95; top10Rate=0.8667<1.0; meanReciprocalRank=0.8667<0.97; top1=0.8667<0.95; top10=0.8667<1; mrr=0.8667<0.97 |
| `context-search-probe-4` | 30 | 24/30 | 24/30 | 0 | false | top1Rate=0.8<0.95; top10Rate=0.8<1.0; meanReciprocalRank=0.8<0.97; top1=0.8<0.95; top10=0.8<1; mrr=0.8<0.97 |
| `context-search-probe-6` | 30 | 23/30 | 23/30 | 0 | false | top1Rate=0.7667<0.95; top10Rate=0.7667<1.0; meanReciprocalRank=0.7667<0.97; top1=0.7667<0.95; top10=0.7667<1; mrr=0.7667<0.97 |
| `context-search-probe-7` | 40 | 33/40 | 33/40 | 0 | false | top1Rate=0.825<0.95; top10Rate=0.825<1.0; meanReciprocalRank=0.825<0.97; top1=0.825<0.95; top10=0.825<1; mrr=0.825<0.97 |
| `context-search-unseen-20260826` | 100 | 58/100 | 80/100 | 1 | false | top1Rate=0.58<0.68; top10Rate=0.8<0.94; meanReciprocalRank=0.6628<0.77; top1=0.58<0.68; top10=0.8<0.94; mrr=0.6628<0.77; top1=58<68; top10=80<94; cohort:application-api=8<9; cohort:behavior-to-test=5<10; cohort:domain-invariants=8<12 |
| `context-search` | 60 | 43/60 | 43/60 | 0 | false | top1Rate=0.7167<0.85; top10Rate=0.7167<0.95; meanReciprocalRank=0.7167<0.85; top1=0.7167<0.95; top10=0.7167<1; mrr=0.7167<0.97 |

All ten affected Dashboard search IDs remain in top10; nine are top1 and unseen-v2-011 is rank3. This current result does not prove unchanged before/after ranking. Probe3 has26 hits out of **30**, not26/26.

Full artifact/corpus SHA-256 fingerprints, every violated threshold/cohort and Dashboard ID/old/new target appear in dashboard-wiki-supplemental-evals.json. Raw JSON and process receipts live in .artifacts/dashboard-evidence.

The exact-base Git tree proves stale expected paths for all current misses in generalization, holdout, probe3/4/6/7 and primary, and15 of20 unseen misses. Examples: central WebPushEndpointValidationHandler, NotificationWebPushOutboxProcessor and AchievementEvaluationOutboxProcessor expected paths were already absent at base; current ranking finds module implementations. Existence evidence does **not** establish numerical baseline scores. No non-Dashboard target was corrected. Details: supplemental-corpus-findings.json in evidence.

Development-context evaluation is separate: process exit0 but JSON passed=false. Before the approved InferLayer fix, Dashboard is found in scope/top10 with complete data/checks but lacks Infrastructure in EffectiveLayers. Other failures concern Favorites target, Notification retrieval and context-snapshot focused checks. They remain unmodified findings with numerical baseline unknown. Both pre/post helper results are retained.

Approved module layer inference correction: the focused public GetDevelopmentContextAsync regression failed before the fix (9 failed/19 passed, 28 selected) and the full MCP suite passed after it (220 passed, zero failed/skipped). InferLayer now recognizes exact Modules/<nonempty module>/Application, Domain and Infrastructure segments, including Abstractions/Model descendants, without changing legacy roots or inventing Contracts semantics. Negative tests exclude test paths, unknown folders and prefix lookalikes. No ranking, queries, thresholds or CLI exit semantics changed.

The unchanged development-context corpus rerun completed with process exit0 and JSON passed=false. Dashboard dashboard-meal-projection-bundle now has expectedLayersPresent=true and contextBundleReady=true; expectedLayersRate becomes1 and readiness .75. Favorites, Notification and context-snapshot findings remain unmodified and their numerical baseline is UNKNOWN. See development-context-after.json and mcp-final.log/results/mcp-final.trx under .artifacts/dashboard-evidence. Product runtime suites were not repeated for this metadata-only fix.

The actual facade stopped at frozen100 with measured exact-baseline equivalence (94/100 top1,98/100 top10,errorCapture=.5); that exception is limited to this gate. Eight supplemental corpora also have semantic failures with numerical baseline UNKNOWN. Their process exits, denominators, every threshold/cohort violation, fingerprints and Dashboard old/new targets are documented in dashboard-wiki-supplemental-evals.md/json. Neither successful process exits nor source relocation hashes establish retrieval quality equivalence. This extraction does not claim full Wiki green or that only one quality failure exists.
