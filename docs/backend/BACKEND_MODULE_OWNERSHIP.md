# Backend module ownership

## Purpose

This document defines business-module ownership inside the primary FoodDiary modular monolith. Project/layer boundaries remain defined by `docs/ARCHITECTURE.md`; this document adds the vertical business boundaries inside those layers.

The rules are intentionally evolutionary:

- one deployment unit and one primary PostgreSQL database remain valid;
- a module owns behavior and writes to its data even when tables share a `DbContext`;
- cross-module reads use an explicit read service/model contract;
- cross-module writes use the owning module's command/service contract or an event;
- another module must not inject the owner's repositories merely because they are available through DI;
- new assemblies, schemas or network calls are not required to establish ownership.

## Interaction types

| Interaction | Use when | Constraint |
| --- | --- | --- |
| Module command/service | The caller needs an immediate action | Contract is owned by the called module; caller must not mutate its repositories directly |
| Module query/read service | The caller needs current data | Return a stable projection, not another module's aggregate |
| Domain event | A reaction must add state in the same transaction | Handler must not perform external I/O |
| Integration event/outbox | Another process must observe committed state reliably | Delivery is at-least-once; consumer must be idempotent |
| Composed read model | A screen/report combines several modules | Composition belongs to a dedicated read path and does not transfer write ownership |

## Governed ownership map

The canonical ownership inventory and cross-layer mappings live in `docs/architecture/backend-modules.json`. It unifies 39 folder modules in `FoodDiary.Application` with the extracted Billing and Marketing assemblies (41 primary backend modules total). The executable folder-module API graph remains in `docs/architecture/module-dependencies.json`; architecture tests derive its direct `FoodDiary.Application.<Module>` dependencies with Roslyn syntax traversal and require an exact manifest match. Every new in-process Module API dependency is therefore an explicit architecture decision; unknown modules, self-edges and unacknowledged strongly connected components fail the build.

Generated Wiki pages must keep business-module dependencies, abstraction-contract dependencies, project/host consumers and runtime composition evidence separate. An empty observed edge set is not proof of isolation; enforceability is declared per module in the ownership manifest.

The Application module graph is now acyclic and `knownCycles` is empty. Consumer-owned profile ports removed FavoriteMeals, Dietologist, Notifications and the Dashboard/health-tracking chain from the original strongly connected component. Architecture tests reject any newly introduced cycle.

This map covers the governed business owners and composed read modules in the primary backend. New areas must be classified before they introduce persistence or cross-module dependencies; absence from the table never grants shared write ownership.

| Module | Owns aggregates/data | Public application surface | Approved collaborators |
| --- | --- | --- | --- |
| Users | user profile/lifecycle, credentials stored on User, roles and role audit | narrow profile, authentication, administration, notification and billing capabilities; current-user access; role membership | Images, Notifications/Profile composition, Dietologist relationship projection |
| Identity | login/register/restore workflows, refresh sessions, login events and email templates | authentication commands, token/session lifecycle and email-template administration, grouped in `FoodDiary.Application.Identity` | Users authentication capabilities, Notifications, MailRelay and external identity validators |
| Consumption Diary | `Meal`, meal items, AI sessions and consumption mutations | consumption commands, `IConsumptionReadService`, specialized activity/nutrition/export projections | Products/Recipes lookup APIs, Users, Images, FavoriteMeals, RecentItems, Nutrition |
| RecentItems | recent product/recipe usage ordering | `IRecentItemUsageReadService`, `IRecentItemUsageRecorder` | Products and Recipes consume usage ordering; Consumption Diary records usage |
| Products | products and product overview/search projections | product commands, `IProductLookupService`, `IProductOverviewReadService` | Users, Images, FavoriteProducts, RecentItems, external food sources |
| Recipes | recipes, ingredients, steps and recipe overview/search projections | recipe commands, `IRecipeLookupService`, `IRecipeAccessService`, `IRecipeOverviewReadService`, `IRecipeNutritionWriter` | Products lookup API, Users, Images, FavoriteRecipes, RecentItems, Nutrition |
| Fasting | fasting plans, occurrences, check-ins, legacy fasting sessions, fasting telemetry | Fasting commands/queries, `IFastingReadService`, analytics and notification scheduling | Users access contract, Notifications contracts; Dashboard is an approved read-only consumer |
| Notifications | notifications, preferences, web-push subscriptions and notification web-push outbox | `INotificationWriter`, notification feed/preferences commands and queries, delivery scheduling contracts | Users access contracts, Resources |
| Billing | subscriptions and billing transaction state | checkout, portal, webhook and entitlement operations | Users identity, billing provider adapter |
| Images | image asset lifecycle and object-deletion outbox | presign, confirm, delete and cleanup operations | Object storage adapter |
| Favorites | user-to-product, user-to-recipe and user-to-meal favorites | favorite commands and read services grouped in `FoodDiary.Application.Favorites` | Products/Recipes lookup APIs, Consumption Diary source-read API, Users access |
| Dietologist Relationships | invitations, permissions and recommendations | relationship/recommendation commands and read services | Users directory/roles, Notifications writer/refresh, Email |
| RecipeCommunity | recipe comments and likes | comment commands/read service and like toggle/status grouped in `FoodDiary.Application.RecipeCommunity` | Recipes access API, Users, Notifications writer |
| Body Metrics | `WeightEntry` and `WaistEntry` measurements | measurement commands, `IWeightEntryReadService`, `IWaistEntryReadService` | Users access; Dashboard, Weekly Check-In and TDEE as read-only projection consumers |
| Hydration | `HydrationEntry` and hydration totals | hydration commands, `IHydrationEntryReadService`, and hydration-goal capability in `FoodDiary.Application.Hydration` | Users access; Dashboard and Weekly Check-In as read-only projection consumers |
| Exercises | `ExerciseEntry` and burned-calorie measurements | exercise commands and `IExerciseEntryReadService` in `FoodDiary.Application.Exercises` | Users access; Dashboard and TDEE as read-only projection consumers |
| Cycles | cycle profile, factors, symptoms, bleeding entries and fertility signals | cycle commands and `ICycleReadService` in `FoodDiary.Application.Cycles` | Users access; Dashboard as a read-only query consumer |
| MealPlanning | meal plans plus shopping lists, items and provenance | MealPlans and ShoppingLists use cases grouped in `FoodDiary.Application.MealPlanning`; `IMealPlanReadService`, `IShoppingListCreationService`, `IShoppingListReadService` | Users access, Recipes/Product projections |
| Wearables | provider connections and synchronization entries | connection/sync commands and `IWearableReadService` | Users access, provider client adapters |
| Marketing Attribution | attribution events and conversion state | attribution command, conversion recorder and summary read service | Authentication and Billing call semantic capabilities |
| Lessons | nutrition lessons and user lesson progress | lesson queries/progress command, `ILessonAdministrationService` | Users access; Admin invokes the owner administration capability and consumes admin projections |
| Daily Advice | localized daily advice content | daily-advice query and `IDailyAdviceReadService` | Dashboard invokes the query as a read-only composed-view dependency |
| Content Reports | user-submitted reports and moderation status | report creation command, `IContentReportAdministrationService`, moderation projections | Users access; Admin invokes owner moderation capability and read projections |
| AI | AI usage, prompt templates and AI orchestration policy | AI use cases, usage summaries, `IAiPromptAdministrationService` | Images access, Users access, provider adapters; Admin consumes projections |
| USDA Catalog | imported USDA foods/nutrients and product-link relationships | USDA commands/read services and read-model search projection | Products may consume the suggestion projection; Consumption nutrition is a read-only calculation input |
| OpenFoodFacts Cache | cached external product documents and refresh lifecycle | cached product-search service in `FoodDiary.Application.OpenFoodFacts` | Products consumes the service; external provider remains an integration adapter |
| Dashboard | composed user-facing read model; owns no source aggregates | dashboard query/read services | Approved read models from contributing modules |

## Fasting pilot boundary

Fasting was the first module migrated to an executable vertical-boundary guardrail and remains the reference example for the migration pattern.

### Ownership

Fasting exclusively owns mutation of:

- `FastingPlan`;
- `FastingOccurrence`;
- `FastingCheckIn`;
- `FastingSession`;
- `FastingTelemetryEvent`.

Its repository and cross-module read contracts live under `FoodDiary.Application.Abstractions/Fasting`, domain behavior under `FoodDiary.Domain/Entities/Tracking/Fasting`, application behavior under `FoodDiary.Application.Fasting`, and EF implementations/configuration under Fasting-specific infrastructure folders.

Other application modules must not acquire Fasting repositories. Infrastructure implementations live under `Persistence/Tracking`, and their EF configurations under `Persistence/Configurations/Tracking`; this placement is enforced by architecture tests.

### Approved dependencies

Fasting Application may depend on:

- cross-cutting `FoodDiary.Application.Common` behavior;
- Users application/application-abstraction `Common` contracts, including `ICurrentUserAccessService`, to establish the acting user boundary;
- Notifications application/application-abstraction `Common` contracts to create, deduplicate and deliver fasting notifications, including `INotificationClientRefreshService` for post-commit client refresh;
- its own contracts, models and domain types.

It must not acquire repositories or application services from other features without updating this document and the architecture guardrail intentionally.

### Approved consumers

- Presentation maps Fasting HTTP contracts to Fasting commands/queries.
- JobManager invokes Fasting scheduling as an operational adapter.
- Dashboard consumes `IFastingReadService` and Fasting projection models to compose a screen-level read model. It receives no Fasting repositories and has no Fasting write capability.

### Current compromise

`IFastingReadService` and some application models currently live in `FoodDiary.Application`, not `FoodDiary.Application.Abstractions`. This is acceptable for the in-process Dashboard composition today, but it is a visible module API. If more consumers appear, move only the stable public projection contract to Application.Abstractions instead of exposing further internal services.

## Evolution checklist

When changing a module boundary:

1. Identify the owner of every written aggregate/table.
2. Classify each cross-module call using the interaction table above.
3. Prefer an existing narrow contract; do not inject a foreign repository.
4. Add a public module contract only when a real consumer exists.
5. Update this map and an ADR when ownership or allowed dependency direction changes.
6. Add or extend an architecture test before migrating the next module.

## Users assembly boundary

Users owns the `User`, `Role`, `UserRole` and `UserRoleAuditEvent` aggregates. Cross-module profile reads use `IUserProfileReadService` and projection models from `FoodDiary.Application.Abstractions/Users`; consumers must not depend on `FoodDiary.Application.Users.Models`.

The aggregate-oriented `IUserContextService` remains internal to the Users application module. Cross-module consumers use `ICurrentUserAccessService`, `IUserProfileReadService` or another narrow abstraction-level capability instead of acquiring `IUserContextService` or a Users repository. `CurrentUserAccessResolver`, `UserIdParser` and the profile-composition ports also live in `FoodDiary.Application.Abstractions`, so ordinary application modules no longer need the Users implementation namespace merely to validate an acting user.

AI, Dashboard, Dietologist, Gamification, Hydration, TDEE (physically isolated in `FoodDiary.Application.Tdee`) and Weekly Check-In use dedicated projection contracts from `FoodDiary.Application.Abstractions/Users`. Their application services receive only the fields required by their calculation or display behavior; they do not receive the `User` aggregate. Dashboard, Dietologist and Weekly Check-In perform access validation through `ICurrentUserAccessService`; Gamification receives only the immutable calorie schedule required by its calculations. Dietologist receives identity/display fields and role membership through `UserDietologistProfileModel`, including its lookup and notification flows.

The Users application implementation now lives in the dedicated `FoodDiary.Application.Users` assembly. Executable hosts register it through `AddUsersModule()`, while other application modules continue to depend only on abstraction-level capabilities and projection models.

Authentication delegates password authentication, successful-authentication recording, account restoration, Google and Telegram authentication/linking, email-verification token issuance/completion and password-reset issuance/completion to `IUserAuthenticationIdentityService`. Registration and initial-admin bootstrap use `IUserAuthenticationRegistrationService`. Authentication remains responsible for validating provider assertions, replay protection, raw token generation and session/JWT issuance, while Users owns identity uniqueness checks, credential hashing, credential verification, one-time-token validation and aggregate mutation. Authentication handlers receive only results, `UserModel`, delivery data or `UserAuthenticationPrincipalModel`; they never acquire the `User` aggregate.

Admin reads and mutations use `IUserAdministrationReadService` and `IUserAdministrationMutationService`; impersonation obtains only `UserAuthenticationPrincipalModel`. Notifications uses `IUserNotificationProfileService` for preference/context projections and mutations. Billing uses `IUserBillingService` for an immutable billing profile plus trial and Premium-role operations. These boundaries keep aggregate mutation inside Users and are protected by architecture tests.

## Notifications boundary

Notifications is the second module protected by an executable vertical-boundary guardrail.

### Ownership

Notifications exclusively owns mutation of:

- `Notification`;
- `WebPushSubscription`;
- notification preferences stored with the owning user lifecycle;
- `NotificationWebPushOutboxMessage` and its delivery state.

### Public write boundary

Other application modules create notification state through `INotificationWriter` or an explicit Notifications command. They must not acquire Notifications aggregate, lookup, read-model, read or write repositories. Cross-module reads, deduplication and client refreshes use semantic Notifications APIs, including `INotificationDeduplicationService`.

Current producers include Fasting, Dietologist, Authentication and RecipeComments. The writer is intentionally an application port: it preserves a synchronous transactional notification write without exposing persistence ownership.

### Approved dependencies and adapters

Notifications Application may depend on abstraction-level Users access contracts to resolve and validate the notification owner. User preferences and notification context are obtained through `IUserNotificationProfileService`; Notifications never loads the `User` aggregate. JobManager may invoke cleanup and web-push outbox processor contracts. Presentation may implement live `INotificationPusher` and test scheduling adapters. Resources implements notification text rendering. None of those adapters owns notification state.

Web-push delivery uses `IWebPushDeliveryAudienceService`. Notifications resolves user preferences, selects active subscriptions, prunes expired entries and removes provider-rejected subscriptions; the Integrations provider adapter receives only delivery-ready endpoint material. Presentation test scheduling refreshes clients through `INotificationClientRefreshService`, never through notification repositories or direct pusher orchestration.

### Current read-side compromise

Fasting and Dietologist client refreshes now use the semantic `INotificationClientRefreshService` boundary. Users profile composition uses `IWebPushSubscriptionReadService` instead of the subscription read-model repository. These migrated modules are protected from regaining direct Notifications read-model repository dependencies. Remaining consumers must be classified and migrated before the repository contracts can become fully internal to Notifications.

## Billing boundary

Billing is the third module protected by executable vertical-boundary guardrails.

### Ownership

Billing exclusively owns mutation of:

- `BillingSubscription`;
- `BillingPayment`;
- `BillingWebhookEvent`;
- provider and external-payment identifiers associated with those aggregates.

Other application modules must not acquire Billing repositories. Billing persistence implementations live under `Persistence/Billing`, EF configurations under `Persistence/Configurations/Billing`, and the shared `DbContext` exposes Billing sets through its dedicated partial.

### Public capabilities and adapters

- Presentation invokes Billing commands and queries for checkout, portal, trial, overview and webhooks.
- JobManager invokes `IBillingRenewalService`; it must not depend on the concrete renewal implementation.
- Integrations implements provider gateways and public provider configuration.
- Admin consumes its dedicated projection-oriented admin Billing read service, not Billing aggregates.

### Transaction and idempotency boundary

Webhook and renewal workflows use `IBillingTransactionRunner` as a narrow explicit transaction boundary. Provider event IDs and external payment IDs are protected by database uniqueness constraints; duplicate races are translated to already-processed semantics. This is intentionally stronger than a check-before-insert alone.

### Approved collaborators

Billing uses `IUserBillingService` to resolve a billing-specific account projection, start the application trial and synchronize Premium membership without loading or mutating the `User` aggregate. Premium role transitions emit through the Billing-owned `IBillingMarketingConversionRecorder` port, implemented by the extracted Marketing module. New collaborators require an intentional ownership-map and guardrail update.

## Products and Recipes boundaries

Products and Recipes are separate modules, even though both participate in the food catalog experience. They have different aggregates, write lifecycles and consumers. Their shared UI purpose does not transfer data ownership.

### Products ownership and API

Products owns `Product` mutations and product overview/search projections. Other modules must not acquire product aggregate repositories. Consumers such as Meals/Consumptions, FavoriteProducts and ShoppingLists use `IProductLookupService`; presentation-oriented product lists use the overview read service.

Products may collaborate with Users, Images, FavoriteProducts, RecentItems and external catalog sources such as USDA/Open Food Facts through narrow contracts. Its persistence registrations and aggregate configuration are kept in Products-owned infrastructure modules.

### Recipes ownership and API

Recipes owns `Recipe`, `RecipeIngredient` and `RecipeStep` mutations and recipe overview/search projections. RecipeComments, RecipeLikes and FavoriteRecipes remain separate interaction modules and do not gain ownership of the Recipe aggregate.

Other modules use `IRecipeLookupService` or `IRecipeAccessService` for existence/access decisions. Recipe nutrition mutation is exposed through the narrow `IRecipeNutritionWriter` capability instead of the full repository. Recipes uses `IProductLookupService` to validate ingredient product references rather than acquiring a Products repository.

### Infrastructure composition

The historical `AddFoodPersistence` registration remains only as a composition aggregator. It delegates to Products, Recipes, RecentItems and Meals registration modules and may not contain registrations itself. Product and Recipe aggregate EF configurations live under their respective owned configuration folders.

## Consumption Diary and RecentItems boundaries

### Consumption Diary ownership

The application feature is named `Consumptions`, while its domain and persistence vocabulary uses `Meal`. Together they form one Consumption Diary module. It owns `Meal`, `MealItem`, `MealAiSession` and `MealAiItem` mutations.

Other modules must not acquire `IMealRepository`, `IMealReadRepository` or `IMealWriteRepository`. User-facing consumers use `IConsumptionReadService`; reporting and calculation modules use purpose-specific read projections such as consumption, activity and product-nutrition contracts. FavoriteMeals now reads the source meal through `IConsumptionReadService` rather than materializing a foreign aggregate.

Consumption Diary may use Products and Recipes lookup APIs for nutrition resolution, Images for asset lifecycle, Users access contracts, FavoriteMeals projections, RecentItems usage recording and shared Nutrition validation.

### RecentItems ownership

RecentItems owns `RecentItem` and its ordering/update semantics. It exposes two semantic capabilities:

- `IRecentItemUsageReadService` for recent product/recipe ordering;
- `IRecentItemUsageRecorder` for recording usage after consumption mutations.

Products, Recipes and Consumptions must not acquire RecentItems repositories. Repository contracts remain adapter-facing persistence details behind those capabilities.

### Infrastructure placement

Meal persistence registrations live in `DependencyInjection.Meals.cs`, RecentItems registrations in `DependencyInjection.RecentItems.cs`, and their EF configurations in owned `Configurations/Meals` and `Configurations/RecentItems` folders.

## Users and Authentication boundaries

Users and Identity are separate collaborating modules around one identity lifecycle. Users owns the `User`, `Role`, `UserRole` and `UserRoleAuditEvent` aggregates/state, including credential verification and account lifecycle mutation. Identity is physically isolated in `FoodDiary.Application.Identity` and owns login/register/restore orchestration, refresh-token sessions, login-event history and application email-template administration. Authentication and Email remain logical feature areas inside that assembly.

### Users public capabilities

Other modules must not acquire `IUserRepository`, `IUserLookupRepository` or `IUserWriteRepository`. Instead they use intent-specific APIs:

- `ICurrentUserAccessService` and feature-specific projection services for active-user access;
- `IUserRoleMembershipService` for role membership changes;
- `IUserAdministrationReadService` and `IUserAdministrationMutationService` for privileged administration workflows;
- `IUserAuthenticationIdentityService` and `IUserAuthenticationRegistrationService` for authentication, registration, account restoration, credential-token workflows and external identities;
- `IUserNotificationProfileService` for notification preferences/context;
- `IUserBillingService` for billing identity, trial state and Premium-role operations.

The public cross-module capabilities return projection models rather than the `User` aggregate. Aggregate-oriented lookup/write contracts remain persistence ports for Users implementations and adapters, not APIs for neighboring application modules.

### Authentication boundary

Authentication orchestrates credentials, external identity validation, token issuance, email verification/reset and refresh-session lifecycle. Its handlers do not acquire Users repositories or the `User` aggregate: password authentication, registration, restoration and identity mutation flow through Users capabilities that return narrow projections for token issuance.

Refresh sessions and login events remain Authentication-owned persistence contracts. Password hashing, JWT generation, Telegram/Google verification and SSO are adapters behind Authentication abstractions.

### Infrastructure placement

User/role EF configurations live in `Configurations/Users`. Refresh-token session and login-event configurations live in `Configurations/Authentication`. The shared Users `DbContext` partial is a physical persistence convenience and does not merge business ownership.

## Images and Favorites boundaries

Images owns `ImageAsset`, upload validation, cleanup policy and the durable object-deletion outbox. Other modules use `IImageAssetAccessService` and `IImageAssetCleanupService`; they must not acquire image repositories. AI image analysis now resolves and validates ownership through the Images capability rather than loading `ImageAsset` persistence directly.

FavoriteProducts, FavoriteRecipes and FavoriteMeals are three cohesive feature slices inside the single physical `FoodDiary.Application.Favorites` module. Each slice owns its favorite entity and exposes commands plus a semantic read service, while sharing one composition boundary because they have the same lifecycle, dependency profile and relationship-focused responsibility. Products, Recipes and Consumption Diary consume those read services rather than favorite repositories.

FavoriteMeals uses the narrow `IFavoriteMealSourceReadService` abstraction when creating a favorite from a source meal. Consumption Diary implements that source projection. Consumption Diary also consumes the abstraction-owned `IConsumptionFavoriteReadService`, implemented by Favorites for favorite IDs and overview projections. Both projects depend on contracts in `FoodDiary.Application.Abstractions`, so the physical Application projects remain acyclic and avoid mutual repository or project references.

Image EF configuration and object-deletion outbox configuration live in `Configurations/Images`. Favorite relationship configurations live together in `Configurations/Favorites`, while their repositories remain separated by owned persistence folders.

## Dietologist relationships and recipe social boundaries

Dietologist Relationships owns invitations, relationship permissions and recommendations. Users profile composition consumes `IDietologistInvitationReadService`; it no longer queries the Dietologist read-model repository directly. Notification production uses Notifications writer/refresh capabilities, while role changes use the Users role-membership capability.

RecipeCommunity is the physical application module for the separate RecipeComments and RecipeLikes logical feature areas. Both may use `IRecipeAccessService` to validate the target recipe, but neither owns or loads the Recipe repository directly. Other modules consume their read services and commands rather than comment/like repositories.

Dietologist invitation/recommendation configurations live in `Configurations/Dietologist`. Recipe comment/like configurations live in `Configurations/RecipeSocial`; repository ownership remains separated in the corresponding persistence folders.

## Health Tracking boundaries

Health Tracking is a product area composed of four independently owned modules rather than one shared write model:

- Body Metrics owns `WeightEntry` and `WaistEntry`;
- Hydration owns `HydrationEntry`;
- Exercises owns `ExerciseEntry`;
- Cycles owns cycle profiles, factors, symptom and bleeding entries, and fertility signals.

Only the owning module may acquire its aggregate or write repositories. Other modules perform mutations through commands and consume stable read services or dedicated projection contracts. Sharing a tracking screen or contributing to the same health calculation does not grant repository ownership.

Dashboard is a composed read model. Its production infrastructure adapter may query the shared database to build an optimized projection, but it owns none of the contributing aggregates and exposes no mutation capability. The application fallback read path is likewise projection-only. Weekly Check-In, TDEE and Gamification are calculation/read modules: they may consume Body Metrics, Hydration, Exercises, Consumption Diary and Dashboard statistics projections, but must not acquire their aggregate or write repositories.

The Dashboard application fallback now consumes `IWeightEntryReadService`, `IWaistEntryReadService`, `IHydrationEntryReadService` and `IExerciseEntryReadService`; it no longer acquires even read repositories from Health Tracking. Health repository isolation is therefore complete for all foreign Application modules.

Body Metrics configurations live in `Configurations/BodyMetrics`, while Hydration, Exercises and Cycles use their corresponding owned configuration folders. Architecture tests protect both application repository boundaries and configuration placement.

## Planning, Wearables and Marketing boundaries

MealPlanning is the shared physical application boundary for the separate MealPlans and ShoppingLists logical owners. Meal Plans reads its own aggregate and submits a `ShoppingListCreationRequest` through `IShoppingListCreationService`; Shopping Lists alone constructs and persists the `ShoppingList` aggregate. Grouping the related planning workflow in one assembly does not transfer write ownership between its aggregates. Their EF configurations remain separated in `Configurations/ShoppingLists` and `Configurations/MealPlans`.

Wearables owns provider connections and synchronization history and is physically isolated in `FoodDiary.Application.Wearables`. The assembly registers its handlers and internal read service through `AddWearablesModule`; executable composition roots reference it explicitly, while core `FoodDiary.Application` has no reverse project reference. Provider APIs remain infrastructure/integration adapters. Connection and sync repositories are private to the Wearables application module, and their configurations live in `Configurations/Wearables`.

Marketing owns attribution events and conversion state and is physically isolated in `FoodDiary.Application.Marketing`. The assembly registers its handlers and services through `AddMarketingModule`; executable composition roots reference it explicitly, while core `FoodDiary.Application` has no reverse project reference. Authentication records attribution through its command API, Billing emits premium conversion through a consumer-owned port, and reporting uses the attribution summary read service. The persistence configuration remains in `Configurations/Marketing`.

Operational cleanup remains owned by the corresponding application module. JobManager invokes `IMarketingAttributionCleanupService` and `IAuthenticationLoginEventCleanupService`; batching and repository access stay inside Marketing and Authentication. A JobManager architecture guardrail prohibits jobs from acquiring any `I*Repository` contract.

## Lessons and Daily Advice boundaries

Lessons owns both `NutritionLesson` and `UserLessonProgress`. Admin does not acquire the Lessons write repository: create, update, delete and bulk import flow through `ILessonAdministrationService`, which keeps aggregate construction, tracking loads and persistence inside Lessons. Admin may consume the dedicated lesson read-model projection for management screens. Lesson and progress configurations live in `Configurations/Lessons`.

Daily Advice is a read-oriented content module that owns `DailyAdvice` storage and selection behavior. Dashboard composes it by sending the Daily Advice query; it does not acquire the repository. Its configuration lives in `Configurations/DailyAdvices`.

## Persistence configuration ownership

Direct acquisition of `FoodDiaryDbContext` is confined to `FoodDiary.Infrastructure/Persistence`, EF migrations/design-time infrastructure and the persistence composition root. Provider caches and lifecycle jobs that query EF are persistence adapters and live under their owning module folders (`Persistence/Ai`, `Persistence/Email`, `Persistence/Users`). An architecture test prevents application-neutral `Services` or future technical folders from becoming alternate data-access layers.

Every EF entity configuration is grouped under an owning module folder. The `Persistence/Configurations` root must contain no loose configuration classes; an architecture test enforces this invariant. Shared use of `FoodDiaryDbContext` therefore remains a physical deployment choice rather than an implicit shared-ownership signal.

Technical and catalog adapters use explicit folders as well: `Admin`, `Ai`, `ContentReports`, `Email`, `Notifications`, `Nutrition`, `OpenFoodFacts` and `Usda`. Folder placement identifies the lifecycle owner or adapter boundary; it does not allow those modules to bypass the application dependency rules.

Executable hosts, Presentation, Initializer, JobManager and Integrations may not inject repository contracts. They invoke application capabilities or implement external ports. This is enforced across all primary backend adapter projects by a single architecture guardrail.

Repository-shaped cross-module projections are no longer allowlisted. Admin reporting/content/audit screens, Gamification, Weekly Check-In, Export, USDA micronutrient calculation and USDA product suggestions all use semantic owner capabilities. Architecture tests reject new foreign repository consumers, including read-model repositories.

Consumption Diary exposes `IMealActivityReadService`, `IConsumptionExportReadService` and `IMealProductNutritionReadService` for calculation/reporting consumers. USDA exposes `IUsdaProductSuggestionReadService` to Products. These APIs preserve optimized read paths without exporting persistence vocabulary.

## Administrative content capabilities

Admin is an orchestration and management surface, not the owner of every entity visible in its screens. Content Reports owns moderation transitions through `IContentReportAdministrationService`; AI owns prompt-template upserts through `IAiPromptAdministrationService`; Email owns template upserts through `IEmailTemplateAdministrationService`. Admin consumes owner-side administration read capabilities for grids, summaries and detail screens and does not acquire these modules' repositories.

The legacy email repository abstractions still live under the `Admin` abstraction namespace. Runtime ownership is nevertheless assigned to Email and enforced at the Application boundary. Moving those adapter-facing contracts can be done separately when the namespace churn is justified; it is not required to preserve the behavioral boundary.

## External catalog boundaries

USDA and OpenFoodFacts are separate catalog adapters with separate cache/import lifecycles. USDA owns imported food/nutrient data and USDA-product link persistence. Products may consume the USDA read-model suggestion projection, while other consumers use USDA read services; they may not acquire the USDA aggregate or link repositories. OpenFoodFacts owns its cached-product repository and exposes cached search behavior rather than its persistence contracts.
