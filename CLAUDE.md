# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Food Diary is a full-stack nutrition tracking application with:
- **Frontend**: Angular 21 SPA with standalone components and Signals
- **Backend**: .NET 10 Clean Architecture API with CQRS via MediatR
- **Database**: PostgreSQL with EF Core
- **Public URL**: https://fooddiary.club

## Build & Development Commands

### Frontend (from `FoodDiary.Web.Client/`)
```bash
npm install                    # Install dependencies
npm run start                  # Dev server at localhost:4200
npm run build:prod             # Production build
npm run lint:fix               # ESLint auto-fix
npm run stylelint:fix          # Style linting
npm run prettier:fix           # Format code
npm run test                   # Run Karma/Jasmine tests
npx ng build fd-ui-kit         # Build shared UI component library
```

### Backend (from repo root)
```bash
dotnet build FoodDiary.slnx                   # Build all projects
dotnet run --project FoodDiary.Web.Api        # Start API at localhost:5000

# Database migrations
dotnet ef migrations add <Name> --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api
dotnet ef database update --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api
```

### EF Migration Post-Processing (CRITICAL)

`dotnet ef migrations add` generates Allman-style braces, but this project enforces **K&R style** via `.editorconfig` with `TreatWarningsAsErrors`. After generating a migration, you MUST fix the `.cs` file:

1. **Remove `using System;`** — implicit usings are enabled, this line triggers IDE0005 error
2. **Convert ALL Allman braces to K&R** — every `)\n{` and `=>\n{` must become `) {` / `=> {`. The most commonly missed pattern is `constraints: table =>\n{` — fix it to `constraints: table => {`
3. **Strip UTF-8 BOM** if present — the Write tool may add BOM, but `.editorconfig` requires `charset = utf-8` (without BOM)
4. **Ensure LF line endings** — `.editorconfig` requires `end_of_line = lf`

Quick fix command after generating a migration:
```bash
# Fix K&R braces, remove BOM, ensure LF (replace MIGRATION_FILE with actual path)
node -e "const fs=require('fs');const f=process.argv[1];let c=fs.readFileSync(f,'utf8');if(c.charCodeAt(0)===0xFEFF)c=c.slice(1);c=c.replace(/\r\n/g,'\n').replace(/^using System;\n/,'').replace(/\)\s*\n\s*\{/g,') {').replace(/=>\s*\n\s*\{/g,'=> {').replace(/(\w)\s*\n\s*\{/g,'\$1 {');fs.writeFileSync(f,c);" MIGRATION_FILE
```

### API Contract Snapshots (CRITICAL)

When adding/changing controllers, HTTP responses, or new fields on existing responses, you MUST update **both** snapshot files:
1. **OpenAPI (Swagger) snapshot** — tests controller routes and schemas
2. **Payload contract snapshot** — tests actual JSON response shapes

Run this after any API contract change:
```bash
UPDATE_CONTRACT_SNAPSHOTS=1 dotnet test tests/FoodDiary.Web.Api.IntegrationTests --no-restore
```
This updates all snapshot `.json` files under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`. Commit the updated snapshots together with the feature.

## Architecture

### Backend (Clean Architecture)
```
FoodDiary.Domain/           # Entities, ValueObjects, Enums (no dependencies)
FoodDiary.Contracts/        # Request/Response DTOs as records
FoodDiary.Application/      # CQRS handlers, validators, business logic
FoodDiary.Infrastructure/   # EF Core, repositories, JWT, S3
FoodDiary.Web.Api/          # Controllers, DI configuration
```

Key patterns:
- **CQRS**: Commands/Queries via MediatR with separate handlers
- **Strongly-typed IDs**: ValueObjects like `UserId`, `ProductId` instead of raw Guid
- **Repository pattern**: Abstract data access in Application, implement in Infrastructure
- **FluentValidation**: Pipeline behaviors validate commands/queries
- **Layer DI**: Each layer exposes `AddApplication()`, `AddInfrastructure()` extension methods

### Frontend
```
FoodDiary.Web.Client/
├── src/app/
│   ├── components/         # Standalone Angular components by feature
│   ├── services/           # API and state management (providedIn: 'root')
│   ├── interceptor/        # JWT token injection
│   ├── guards/             # Route protection
│   └── directives/         # Reusable behaviors
├── projects/fd-ui-kit/     # Shared UI component library (button, card, input, dialog, etc.)
└── src/environments/       # Environment configs (dev, staging, prod)
```

Key patterns:
- **Signals**: Use `signal()`, `computed()`, `input()`, `output()` for state and component APIs
- **Standalone components**: No NgModules, all components are standalone
- **OnPush change detection**: Default for all components
- **Native control flow**: Use `@if`, `@for`, `@switch` (not `*ngIf`, `*ngFor`)
- **Service injection**: Use `inject()` helper, not constructor injection
- **Import from fd-ui-kit**: Prefer `fd-ui-kit/...` imports for shared UI components

## Code Conventions

### Angular/TypeScript
- Strict TypeScript with `strict: true`
- Avoid `any`, prefer `unknown` when type is uncertain
- Host bindings go in `host` object, not decorators
- Keep components small and focused
- Lazy-load feature routes

### .NET
- Controllers stay thin, delegate to MediatR handlers
- Business logic in Application layer, never in controllers or Infrastructure
- Nullable enabled, use structured results for error handling
- Commit both migration files (`.cs` and `.Designer.cs`)

## Features Documentation

- [Dietologist Feature](DIETOLOGIST_FEATURE.md) — Professional nutritionist role with client data access, invitations, granular permissions, recommendations, and notifications

## Legacy Code

`backend/food-diary.web.api/` contains the legacy NestJS backend. The .NET backend is the active one.
