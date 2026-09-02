# Images logical module

Images owns the dependency-free `ImageAssetId` contract, the `ImageAsset` domain
entity, application policies, persistence mapping and repository adapters. Preserve
their legacy CLR namespaces. ID-only consumers reference Images Contracts; Images Domain
references Users Domain for User and Users Domain.Contracts for UserId.

`MealAiSession` retains only `ImageAssetId`; Meals and Dashboard resolve image URLs
through persistence read joins. Shared DbContext, migrations/snapshot, storage
providers and generic deletion outbox processing keep their existing shared owners. UserCleanup belongs to Modules/Users/Infrastructure.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
