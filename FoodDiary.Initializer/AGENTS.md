# Initializer Guidelines

## Scope
Rules for `FoodDiary.Initializer/`.

## Role
- Treat this project as a thin console host for operational database tasks.
- Keep EF Core migrations, `DbContext`, and persistence mappings in `FoodDiary.Infrastructure`.
- Use this project to orchestrate migration apply/rollback, seed routines, and one-off backfill tasks.

## Architecture
- Do not move business rules or domain behavior into this project.
- Reuse `FoodDiary.Application` handlers/services and `FoodDiary.Infrastructure` implementations instead of duplicating logic.
- Keep commands explicit, operational, and safe to run from CI/CD or server shells.

## Structure
- Entrypoint: `Program.cs`
- Project-specific usage notes: `README.md`

## Commands
- Build: `dotnet build FoodDiary.Initializer/FoodDiary.Initializer.csproj`
- Status: `dotnet run --project FoodDiary.Initializer -- status`
- List migrations: `dotnet run --project FoodDiary.Initializer -- list`

## Operational Practices
- Prefer additive corrective migrations over rollback/reapply in shared or production environments.
- Keep destructive operations explicit and parameterized.
- When adding seed or backfill flows, make them idempotent where practical.
