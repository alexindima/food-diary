# Build-Time Analyzer Guidelines

## Scope

Rules for `FoodDiary.Analyzers/`.

## Role

- Keep this project limited to FoodDiary-specific Roslyn diagnostics evaluated during compilation.
- Use analyzers for local syntax or semantic rules that can be decided from one compilation and benefit from immediate IDE feedback.
- Keep repository topology, project-reference, cross-file contract, deployment, and filesystem-layout rules in `tests/FoodDiary.ArchitectureTests`.
- Do not add runtime application code, business rules, application extension methods, or production dependencies here.

## Diagnostic Rules

- Assign stable sequential IDs in the `FDxxxx` range and never reuse a released ID for different behavior.
- Keep new diagnostics disabled by default and activate them for intentional scopes in the root `.editorconfig`.
- Preserve existing framework and generated-code exceptions when migrating a guardrail from architecture tests.
- Report the smallest actionable source location and write a message that tells the developer how to fix the violation.
- Enable concurrent execution and skip generated code unless a rule explicitly governs generated artifacts.
- Update `AnalyzerReleases.Unshipped.md` whenever a diagnostic is added or changed.

## Compatibility

- Target `netstandard2.0` so the compiler can load the analyzer broadly.
- Build against a `Microsoft.CodeAnalysis` version no newer than the Roslyn compiler shipped by the repository's current .NET SDK.
- Analyzer references from production projects must remain build-only (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`).

## Tests

- Add focused positive and negative cases under `Tooling/tests/FoodDiary.Analyzers.Tests` for every diagnostic.
- Cover documented exceptions explicitly, including framework entrypoints, partial types, generated paths, or scoped activation where applicable.
- Verify both the analyzer test project and a real consuming project or solution build when analyzer wiring changes.

## Commands

- Build: `dotnet build FoodDiary.Analyzers/FoodDiary.Analyzers.csproj`
- Focused tests: `dotnet test Tooling/tests/FoodDiary.Analyzers.Tests/FoodDiary.Analyzers.Tests.csproj`
- Consumer verification: `dotnet build FoodDiary.slnx`

FD0015 and FD0016 apply to module Infrastructure assemblies and source paths.
The compiler consumes persistence-technical-sources.txt, whose entries must exactly
match reviewed technicalSourceSha256 values in persistence-capabilities.json.
A technical exception never grants foreign module aggregate writes. Shared audit
persistence writes require that same exact file fingerprint. This is a build-time
architecture guard, not a database authorization boundary.

FD0015/FD0016 inspect method references as well as invocations: delegates must not acquire untyped/foreign EF writes or unreviewed technical save capabilities.
