# RecipeCommunity module

Own RecipeComments and RecipeLikes as separate feature groups. Application keeps its legacy assembly and CLR namespaces. Ports and errors live in Application/Abstractions; entities and IDs in Domain; repositories in Infrastructure/Persistence; EF configurations in Infrastructure/Model.

Domain references the Users and Recipes owners one-way for User/Recipe and their IDs. Neither has an inverse CLR navigation to these entities. Preserve existing navigation properties, WithMany mappings, cascade behavior and the unique user/recipe like index. Recipes remains a separate Domain owner; do not transfer its ownership into RecipeCommunity or introduce provider behavior changes.

Hosts compose AddRecipeCommunityModule; shared DbContext applies ApplyRecipeCommunityPersistenceModel explicitly. Shared migrations/snapshot, HTTP, cross-module PostgreSQL tests and ContentReports reportability stay with their current owner. Errors.RecipeComment remains a central compatibility facade over module-owned errors. No separate Contracts layer is justified by current consumers.

Preserve current-user and recipe access contracts, author/recipe-owner deletion rules, pagination, cancellation and transaction boundaries. Comment notification creation stays through Notifications INotificationWriter. Do not alter shared outbox or provider infrastructure.

Focused application and comment domain tests live in tests under this module. Mixed SocialInvariantTests and PostgreSQL suites remain central. Run focused suites, full ArchitectureTests, relevant donor/consumer suites, PostgreSQL relational operations and EF pending-model check for persistence moves. Keep all artifacts under repository .artifacts/recipecommunity-extraction; no coverage collectors.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
