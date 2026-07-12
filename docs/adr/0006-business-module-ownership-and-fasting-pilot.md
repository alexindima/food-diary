# ADR 0006: Business Module Ownership And Fasting Pilot

## Status

Accepted

## Context

The primary backend has strong horizontal Clean Architecture boundaries, but business-feature ownership inside those layers was previously implicit. A shared `DbContext` and in-process DI make it technically possible for one feature to acquire another feature's repository, gradually turning a modular monolith into a coupled monolith.

Creating an assembly, schema or service for every feature would add migration and operational cost before module boundaries are stable.

## Decision

- Define business-module ownership independently from physical database/project separation.
- The owner is the only module allowed to mutate its aggregates and tables.
- Cross-module reads use explicit projection/read-service contracts.
- Cross-module actions use the owning module's command/service contract or an event.
- Start enforcement with Fasting as the pilot module.
- Extend the same enforcement model to Notifications after validating the pilot.
- Protect Billing ownership and expose recurring renewal to JobManager through `IBillingRenewalService` rather than a concrete application service.
- Treat Products and Recipes as separate modules, protect their aggregate repositories, and split the historical combined food-persistence registration into owned registration modules.
- Treat Consumptions plus the Meal domain/persistence model as one Consumption Diary module, and expose RecentItems through semantic usage read/recording capabilities rather than repositories.
- Separate Users ownership from Authentication orchestration: foreign modules use `IUserDirectoryService` and Users-owned mutation capabilities rather than core User repositories.
- Protect Images and the three Favorite relationship modules behind access/cleanup/read capabilities; remove AI, Consumption Diary and FavoriteMeals cross-repository access.
- Protect Dietologist Relationships, RecipeComments and RecipeLikes; Users profile composition uses the Dietologist relationship read service rather than its repository.
- Treat Body Metrics, Hydration, Exercises and Cycles as separate Health Tracking owners; Dashboard, Weekly Check-In and TDEE may consume read projections but may not acquire their aggregate or write repositories.
- Protect Shopping Lists, Meal Plans, Wearables and Marketing Attribution as independent owners. Meal Plans generates a shopping list through `IShoppingListCreationService`, leaving aggregate construction and persistence inside Shopping Lists.
- Protect Lessons and Daily Advice as content owners. Admin lesson mutations use `ILessonAdministrationService`; admin read screens may retain a projection-only lesson read model.
- Treat Admin as a management surface rather than a universal content owner: Content Reports, AI prompt templates and Email templates expose dedicated administration capabilities while allowing projection-only admin reads.
- Protect USDA and OpenFoodFacts persistence as external-catalog boundaries; Products may consume purpose-specific search projections but not their repositories.
- Keep JobManager as an operational composition host: recurring jobs invoke application capabilities, including Marketing attribution and Authentication login-event cleanup, and may not acquire repositories.
- Prohibit repository injection across primary hosts and adapters. Web-push delivery receives a Notifications-owned delivery audience, and Presentation refreshes notification clients through a semantic capability.
- Complete Health Tracking read isolation by moving Dashboard fallback composition to semantic read services, and maintain an executable file-level allowlist for the remaining cross-module reporting projections.
- Replace Consumption Diary and USDA repository-shaped cross-module projections with semantic activity, export, micronutrient-input and product-suggestion capabilities.
- Allow Fasting to use Users current-access and Notifications contracts.
- Allow Dashboard to consume the Fasting read service/models as a read-only composed-view dependency.
- Do not introduce new assemblies, schemas, brokers or facades solely to claim modularity.
- Require every EF entity configuration to live in an owning module/adapter folder; keep the configuration root empty and protect it with an architecture test.
- Treat `INotificationWriter` as the public transactional write boundary for notification producers; foreign modules must not acquire Notifications write repositories.

## Consequences

Benefits:

- Vertical boundaries become reviewable and executable without a rewrite.
- Shared infrastructure no longer implies shared business ownership.
- Cross-module coupling is made intentional before more physical isolation is considered.
- The pilot provides a repeatable migration pattern for other modules.

Tradeoffs:

- Some module APIs remain in the Application assembly and rely on architecture tests rather than CLR visibility.
- The ownership map and executable allowlists must evolve whenever a new persisted module or legitimate collaborator is introduced.
- Legitimate new collaborators require an intentional documentation and guardrail update.
- A shared database still permits infrastructure-level joins; ownership rules govern writes, not all reporting queries.
- Read-oriented calculation modules may compose purpose-specific projections across owners, but that exception never grants aggregate mutation rights.
