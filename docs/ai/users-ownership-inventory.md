# Users ownership inventory

## Moved to Modules/Users

- `Application`: all former `FoodDiary.Application.Users` commands, queries, validators, mappings, common helpers, and application services. AssemblyName and RootNamespace remain `FoodDiary.Application.Users`.
- `Infrastructure`: role catalog/membership, current weight/waist providers, user cleanup, and the complete `AddUsersModule` composition entrypoint.
- `Domain`: complete User aggregate and credential/security partials, roles, audit, weight/waist goals, User-specific value objects, states, updates, events and goal/role identifiers.
- `Domain.Contracts`: UserId, with only a shared domain primitives dependency.
- `Infrastructure/Model`: EF configurations for User, Role, UserRole, UserRoleAuditEvent, WeightGoal and WaistGoal, applied through the existing explicit model-builder seam from the central DbContext.
- `tests`: focused Users application and domain suites. Mixed or cross-module tests remain with their proven owners.

## Central compatibility seams

- `FoodDiary.Application.Abstractions/Users` remains central. An attempted physical move exposed a circular Authentication dependency and 272 consumer compile errors; these broadly consumed contracts cannot move one-way without first splitting consumer-specific ports.
- `FoodDiary.Domain` retains shared guards, constants, enums and value objects. It contains no entity definitions or reverse module dependencies. User no longer has foreign inverse collections; its goals and role relationships remain unchanged in Users Domain.
- `FoodDiary.Infrastructure/Persistence/Users/UserRepository.cs` remains central because it implements both Users capabilities and Identity/security access surfaces. Splitting it is a separate behavioral refactor.
- The shared DbContext, migrations, and model snapshot remain central. EF reports no pending model changes, so no migration belongs to this extraction.

## Explicit Identity boundary

Credential and security state are inseparable partials of User and move unchanged
with that aggregate. Authentication ports/services, external login, JWT/token
issuance, refresh-token sessions, login events and email templates retain their
existing Identity/provider owners. This is not an authentication redesign. Roles
and role audit remain Users-owned; authorization continues consuming the same
capabilities. See `users-domain-extraction.md` for the current Domain boundary,
residual shared types, coordinated rebuild requirement and verification evidence.
