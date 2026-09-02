# ADR 0027: Retire the residual shared domain assemblies

Status: Accepted 2026-09-02

## Context

After aggregate extraction, FoodDiary.Domain contains only shared value contracts and three length constants. The subsequently extracted FoodDiary.Nutrition.Domain holds a food-quality formula, its grade and measurement units. The user explicitly selected retirement of both production assemblies and accepted concrete Products Domain dependencies for the shared formula. This supersedes the separate Nutrition-library decision in [the earlier extraction record](../ai/nutrition-domain-extraction.md); that design was a deliberate intermediate boundary, not a runtime defect.

## Decision

| Types | Existing owner |
| --- | --- |
| MeasurementUnit | Products Domain.Contracts |
| FoodQualityScore, FoodQualityGrade | Products Domain |
| HealthAreaScore, HealthAreaGrade, HealthAreaScores | USDA Domain |
| DesiredWeightKg, DesiredWaistCm | Users Domain |
| LanguageCode | Users Domain.Contracts |
| EmailAddress, Visibility | Shared Domain.Primitives |
| CycleId | Cycles Domain |

EmailAddress and Visibility adopt the required FoodDiary.Domain.Primitives namespace. Other moved types preserve their legacy CLR namespaces. Keep method bodies, exceptions, numeric enum values and existing language policy unchanged. DomainConstants is dissolved into owner-local 2048-character comment/image limits and 65536-character JSON limits. No new shared replacement assembly, forwarding wrapper, algorithm copy or dynamic loading is introduced.

Products already references Users and USDA; neither may depend back on Products. The two Domain.Contracts projects continue to reference only Primitives. References to Products Domain for food scoring are accepted; they do not authorize foreign aggregate mutation. Keeping a separate Nutrition assembly or putting algorithms in Domain.Contracts was rejected by the explicit ownership decision.

## Compatibility and verification

All consumers and hosts require a coordinated rebuild because assembly ownership changes; binary drop-in compatibility is not promised. HTTP enum names/numbers, OpenAPI snapshots, EF entity identities and persisted schemas remain unchanged. No migration or deployment is part of this change. Rollback is a coordinated source/package revert.

All 43 Nutrition cases move to Products. Focused health, desired-value, language and email cases move to existing owner suites; mixed central tests remain. CycleId stays public and retains the original five reflection cases even though no production consumer currently uses it. Exact project matrices, absence guards, source comparisons, full regression runs, EF pending-model checks and package audits provide the verification evidence.

The Wiki synthetic extraction compile-probe must use evaluated references from its real enclosing donor project instead of assuming a central Domain assembly. Preserve the actual extracted-project branch, compile inputs, conditional references and native errors; never cache a failed probe. This is a bounded compatibility adaptation in the readiness tool and its regression test, not a change to Wiki ranking or governance policy.

The readiness regression also exposed an existing consumer-classification defect, reproduced by the unchanged scanner against the exact pre-extraction revision: `Modules/Users/...` was classified as `Modules`. The separately approved repair recognizes `Modules/<owner>/...` generically, preserving legacy and host layouts. Internal mutations remain visible with their operations, and an actual external module mutation continues to block readiness. Assembly inference and mutation-detection rules are unchanged.
