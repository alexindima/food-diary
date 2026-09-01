# Identity ownership inventory

## Moved to Modules/Identity

- `Application`: all legacy `FoodDiary.Application.Identity` Authentication and Email commands, queries, validators, models, parsers, and services. `AssemblyName`, `RootNamespace`, public CLR namespaces, and `AddIdentityModule` remain `FoodDiary.Application.Identity` compatible.
- `tests`: 171 focused Authentication tests. The test assembly retains `FoodDiary.Application.Tests` for existing `InternalsVisibleTo` compatibility.

## Central and collaborating seams

- `FoodDiary.Application.Abstractions/Authentication` and `/Email` remain central because presentation, infrastructure, integrations, Users, Admin, Notifications, JobManager, and provider implementations consume them.
- `User`, `Role`, `UserRole`, `UserRoleAuditEvent`, password/reset/email-confirmation/security-state behavior, and Users-owned application services remain in their current central/Users owners.
- The mixed `FoodDiary.Infrastructure/Persistence/Users/UserRepository.cs` remains central because it implements both Users and Identity/security ports. Splitting it would be a behavioral persistence refactor and risks a dependency cycle.
- Refresh-token sessions, login events, their EF configurations/repositories, `FoodDiaryDbContext`, migrations, and snapshot remain central. No model delta was introduced and no migration is required.
- JWT generation, Google/Telegram validation, Admin SSO/Redis storage, MailInbox/MailRelay bridges, email transport, HTTP controllers/mappings, and executable hosts remain with their existing provider, integration, presentation, and composition-root owners.

## Test ownership

- Moved only focused Authentication tests.
- Kept `UserAuthenticationRegistrationServiceTests` central as Users-owned behavior.
- Kept Admin email-template and login-activity suites central because they exercise mixed Admin/Identity orchestration.
- Provider-backed PostgreSQL, shared repository/EF, presentation, Web API, JobManager, and HTTP integration suites stay with their established projects.
