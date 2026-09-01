# Users ownership inventory

## Moved to Modules/Users

- `Application`: all former `FoodDiary.Application.Users` commands, queries, validators, mappings, common helpers, and application services. AssemblyName and RootNamespace remain `FoodDiary.Application.Users`.
- `Infrastructure`: role catalog/membership, current weight/waist providers, user cleanup, and the complete `AddUsersModule` composition entrypoint.
- `Infrastructure/Model`: EF configurations for User, Role, UserRole, and UserRoleAuditEvent, applied through an explicit model-builder seam from the central DbContext.
- `tests`: the focused Users application suite. Mixed or cross-module tests remain with their proven owners.

## Central compatibility seams

- `FoodDiary.Application.Abstractions/Users` remains central. An attempted physical move exposed a circular Authentication dependency and 272 consumer compile errors; these broadly consumed contracts cannot move one-way without first splitting consumer-specific ports.
- `FoodDiary.Domain/Entities/Users` and related identifiers remain central because User exposes public bidirectional navigations, including `User.Meals`, across many modules. Moving the CLR types would change dependency direction and risk EF/API compatibility.
- `FoodDiary.Infrastructure/Persistence/Users/UserRepository.cs` remains central because it implements both Users capabilities and Identity/security access surfaces. Splitting it is a separate behavioral refactor.
- The shared DbContext, migrations, and model snapshot remain central. EF reports no pending model changes, so no migration belongs to this extraction.

## Explicit Identity boundary

This task does not extract or redesign Identity. Credentials, password/set/reset/change behavior, email confirmation and security state, authentication ports/services, external login, JWT/token issuance, refresh-token sessions, login-event persistence, and authentication email templates remain central. Users application services may orchestrate narrow existing Identity capabilities, but ownership and persistence do not move. Roles and role audit are Users-owned behavior; authentication authorization continues consuming them through existing contracts.
