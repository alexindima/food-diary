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
dotnet build FoodDiary.sln                    # Build all projects
dotnet run --project FoodDiary.Web.Api        # Start API at localhost:5000

# Database migrations
dotnet ef migrations add <Name> --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api
dotnet ef database update --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api
```

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

## Legacy Code

`backend/food-diary.web.api/` contains the legacy NestJS backend. The .NET backend is the active one.
