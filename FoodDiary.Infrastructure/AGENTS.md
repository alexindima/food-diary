# Infrastructure Layer Guidelines

## Scope
Rules for `FoodDiary.Infrastructure/`.

## Responsibilities
- EF Core persistence, external integrations, and technical implementations.
- Implement abstractions declared in upper layers.

## Data Access
- Keep `DbContext` and entity configurations here.
- Use Fluent API for mapping and constraints.
- Keep migrations in this project.

## Rules
- Do not move domain rules from aggregates into persistence code.
- Keep dependency direction inward (Infrastructure depends on Application/Domain, not vice versa).
- Keep retries/logging policies consistent with API composition.

## Commands
- Build: `dotnet build FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`

## EF Core Migrations
- Always commit both files for each migration: `*.cs` and `*.Designer.cs`.
- **CRITICAL**: `dotnet ef migrations add` generates Allman-style braces, but the project requires K&R style. After generating, you MUST:
  1. Remove `using System;` (implicit usings are enabled)
  2. Convert ALL `)\n{` and `=>\n{` patterns to `) {` / `=> {` — especially `constraints: table =>\n{`
  3. Strip UTF-8 BOM if present
  4. Ensure LF line endings (not CRLF)
- Before commit, run a whitespace formatter/check on migration files. Preferred command: `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`. This specifically avoids CI failures like `WHITESPACE: Fix whitespace formatting` in generated migrations.
- See root CLAUDE.md for a one-liner fix command.
