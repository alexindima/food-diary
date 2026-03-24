# Project Review

Date: 2026-03-24

## Summary

The project is above average in structure and engineering discipline. It already has:

- Clear layer separation: `Domain`, `Application`, `Infrastructure`, `Web.Api`
- Feature-first organization in backend layers
- Architecture tests that enforce layering and folder conventions
- Broad backend test coverage, including integration tests
- A reasonably clean API composition root

The main improvement areas are configuration hygiene, CI coverage, consistency of time abstractions, documentation accuracy, and stricter quality gates.

## Priority Findings

### 1. Configuration and secret hygiene

Priority: High

`FoodDiary.Web.Api/appsettings.json` contains sensitive or production-oriented values:

- Local database password in `ConnectionStrings:DefaultConnection`
- Default JWT secret
- Production domains and operational settings

Why it matters:

- Increases risk of accidental secret leakage
- Makes local/dev/prod boundaries less clear
- Encourages configuration drift

Recommendation:

- Keep only safe defaults in repository-tracked config
- Move real values to environment variables, secret stores, or untracked local settings
- Add an `appsettings.Template.json` or similar bootstrap example

References:

- `FoodDiary.Web.Api/appsettings.json`

### 2. CI validates only the .NET side

Priority: High

The current CI workflow runs .NET restore/build/tests, but does not validate Angular linting, styling, tests, or build output.

Why it matters:

- Frontend regressions can pass PR checks
- Deployment can fail later even when CI is green
- Quality checks are uneven across the stack

Recommendation:

- Add a frontend CI job with:
  - `npm ci`
  - `npm run lint`
  - `npm run stylelint`
  - `npm run test -- --watch=false`
  - `npm run build`

References:

- `.github/workflows/ci-tests.yml`
- `FoodDiary.Web.Client/package.json`

### 3. Integration tests do not use the production database engine

Priority: High

API integration tests replace the real database with EF Core InMemory.

Why it matters:

- Does not catch PostgreSQL-specific issues
- Can hide SQL translation problems
- Does not validate relational constraints and realistic query behavior

Recommendation:

- Keep current InMemory tests for fast feedback if useful
- Add a second integration layer for critical flows using PostgreSQL Testcontainers

References:

- `tests/FoodDiary.Web.Api.IntegrationTests/TestInfrastructure/ApiWebApplicationFactory.cs`
- `FoodDiary.Infrastructure/DependencyInjection.cs`

### 4. Time abstraction is only partially adopted

Priority: Medium

`IDateTimeProvider` exists, but many handlers, domain types, and controllers still use `DateTime.UtcNow` directly.

Why it matters:

- Makes edge-case testing harder
- Introduces inconsistency in time-sensitive logic
- Increases risk around day/month boundary behavior

Examples:

- `FoodDiary.Application/Ai/Queries/GetUserAiUsageSummary/GetUserAiUsageSummaryQueryHandler.cs`
- `FoodDiary.Application/Admin/Queries/GetAdminAiUsageSummary/GetAdminAiUsageSummaryQueryHandler.cs`
- `FoodDiary.Web.Api/Features/Hydration/HydrationEntriesController.cs`

Recommendation:

- Use `IDateTimeProvider` consistently in application and presentation layers
- Review whether domain-level timestamps should be injected or created centrally during persistence/event creation

### 5. Documentation is outdated and partially broken

Priority: Medium

The root README appears to have encoding issues and still describes an old backend shape.

Why it matters:

- Misleads contributors about current architecture
- Slows onboarding
- Reduces trust in repository documentation

Observed issues:

- Broken Cyrillic rendering in `README.md`
- Mentions `NestJS + Prisma`
- Mentions `backend/food-diary.web.api`, which is not present
- Frontend README is still the default Angular CLI template and references Angular CLI 18 while dependencies are on Angular 21

Recommendation:

- Rewrite root `README.md` to match the current .NET + Angular architecture
- Save it explicitly as UTF-8
- Replace the frontend template README with project-specific instructions

References:

- `README.md`
- `FoodDiary.Web.Client/README.md`
- `FoodDiary.Web.Client/package.json`

### 6. Quality gates can be stricter

Priority: Medium

.NET shared build settings are minimal, and frontend linting does not currently push hard against `any`.

Why it matters:

- Lowers the chance of catching issues early
- Allows gradual erosion of code quality in utility and infrastructure code

Observed issues:

- No visible shared analyzer/warning-as-error settings in `Directory.Build.props`
- `any` appears in base frontend services and drag/drop infrastructure

Examples:

- `FoodDiary.Web.Client/src/app/services/api.service.ts`
- `FoodDiary.Web.Client/src/app/services/drop-zone.service.ts`

Recommendation:

- Add .NET analyzer settings in shared props
- Consider `TreatWarningsAsErrors` in CI at least
- Add `@typescript-eslint/no-explicit-any` as warning first, then tighten incrementally

References:

- `Directory.Build.props`
- `FoodDiary.Web.Client/eslint.config.js`

## Strengths

- Good architectural separation
- Useful architecture tests that protect design constraints
- Broad backend test suite
- Feature-based organization is already established
- API startup/composition is compact and understandable

## Suggested Improvement Order

1. Fix config hygiene and documentation
2. Add frontend validation to CI
3. Standardize `IDateTimeProvider` usage
4. Add PostgreSQL-backed integration coverage for critical flows
5. Tighten analyzers and frontend typing rules

## Notes

This review was based on repository inspection. Full build/test execution was not confirmed in the current environment because `dotnet` was not available in `PATH` during analysis.
