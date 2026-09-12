# Scalar persistence model boundaries

The accepted AI pilot now extends to Hydration, RecentItems, Exercises and WeeklyGoals. Each of the four models replaces Users.Domain with the narrow Users.Domain.Contracts assembly for UserId. Owned entities, scalar conversions, indexes and constraints remain in their original PersistenceModel projects.

Each module has a typed relationship composer in FoodDiary.Infrastructure/Persistence/Composition, invoked after all owned models in FoodDiaryDbContext.OnModelCreating. HydrationEntry, RecentItem, ExerciseEntry and WeeklyGoal retain their UserId foreign key and Cascade deletion. Central Infrastructure already references all four owning Domain assemblies and Users.Domain; no new composition dependency or public API is needed.

ScalarPersistenceBoundaryTests protects compiled assembly references for the initial five models, including AI. The exact dependency matrix additionally rejects unused direct foreign Domain references. ModuleAggregateIsolationTests protects migration equivalence and navigation isolation. Existing Hydration PostgreSQL tests cover User cascade and caller transaction ownership; RecentItems covers concurrent upsert and retention; WeeklyGoals covers concurrent creation and reminders; the mixed tracking regression covers Exercises persistence.

No database migration, provider call, transaction change or deployment configuration is introduced. Schema equivalence is a release criterion. This strengthens compile-time boundaries while preserving the shared database and DbContext. Other modules retain their current mapping policy until deliberately migrated. Never use string entity names or reflection in production to conceal a foreign model dependency.

## Images, Cycles, BodyMetrics and Wearables

The same boundary now covers ImageAsset, CycleProfile, WeightEntry, WaistEntry, WearableConnection and WearableSyncEntry. Their six UserId foreign keys retain Cascade deletion in four typed central composers. All four PersistenceModel projects reference Users.Domain.Contracts. Central Infrastructure adds an explicit Wearables.Domain reference because it now consumes those entity types directly.

ScalarPersistenceBoundaryTests covers all nine models. ModuleAggregateIsolationTests retains full relational snapshot equivalence. Focused PostgreSQL regression coverage exercises Images ownership/confirmation, Cycles aggregate deletion, BodyMetrics persistence and Wearables concurrent connection/sync creation. No migration, query, credential protection, provider call or transaction ownership changes are intended.

## ContentReports, Lessons, Gamification and Notifications

Four more models use Users.Domain.Contracts instead of Users.Domain. Four typed
central composers preserve five UserId Cascade foreign keys: ContentReport,
UserLessonProgress, UserAchievement, Notification and WebPushSubscription. Central
Infrastructure already references their Domain assemblies. Same-owner lesson and
notification-outbox relationships remain local. No domain, query, transaction,
provider, API or schema changes are introduced.

ScalarPersistenceBoundaryTests covers all thirteen isolated models. The relational
snapshot-equivalence check and focused PostgreSQL report, lesson-progress,
achievement and notification/subscription scenarios protect persistence behavior.

## Fasting and Billing

Both models replace Users.Domain with Users.Domain.Contracts. Central typed
composers preserve six UserId Cascade foreign keys: FastingPlan, FastingOccurrence,
FastingSession, FastingCheckIn, BillingSubscription and BillingPayment. Existing
central Domain references suffice. Payment-to-subscription SetNull and same-owner
Fasting relationships remain local. No migrations, provider calls, transaction
changes or financial workflow changes are introduced.

The compiled boundary guard covers fifteen models. Relational snapshot equivalence
and focused PostgreSQL Fasting and Billing repository scenarios protect the move.

## Admin, Identity and Application consumers

Admin and Identity models now use Users.Domain.Contracts. Typed central composers
preserve AdminImpersonationSession ActorUserId/TargetUserId Restrict foreign keys
and Identity UserLoginEvent/UserRefreshTokenSession UserId Cascade foreign keys.
The compiled scalar-model guard now covers seventeen models. No authentication,
role, session, public contract or relational schema behavior changes.

Billing, BodyMetrics, Dashboard, Fasting, Gamification and Notifications Application
reference Users.Domain.Contracts instead of Users.Domain for scalar types. Their
existing Users.Contracts capabilities remain. BodyMetrics already had the narrow
reference, so its redundant aggregate reference is simply removed. Exact project
allowlists and focused application suites protect these boundaries.

Admin, Identity and Dietologist Application still consume Users.Domain RoleNames.
Moving these role constants requires a separate consumer review including
presentation/authentication code; they are not replaced with duplicated literals.

## RecipeCommunity

RecipeCommunity PersistenceModel references Users and Recipes Domain.Contracts. Central typed composition preserves RecipeLike.UserId, RecipeComment.UserId and RecipeComment.RecipeId Cascade foreign keys. RecipeLike deliberately has no RecipeId FK in the existing schema. The user/recipe unique like index and owned mappings remain local. Central Infrastructure references RecipeCommunity.Domain directly. Eighteen scalar models are guarded; no schema migration or application behavior change is intended.

## Dietologist

Dietologist PersistenceModel references Users.Domain.Contracts. Ten User FKs move verbatim to central DietologistCrossModuleRelationships: seven Cascade, two ClientTask Restrict, and invitation DietologistUserId SetNull with IsRequired(false). Local Recommendation links, indexes, converters and xmin stay in the owner model. Nineteen scalar models are guarded. No migration, access-control, notification or audit behavior change is intended.

## Users

Users PersistenceModel references ID-only Images.Contracts instead of Images.Domain. The optional User.ProfileImageAssetId ClientNoAction FK moves to central UsersCrossModuleRelationships. Owned role/goal relationships, converters and field access remain local. Existing central Users/Images Domain references suffice. Twenty scalar model assemblies are guarded. ADR 0032 image integrity and explicit profile unlinking during user cleanup remain unchanged; no migration or authentication changes.

## Favorites and MealPlanning

Favorites and MealPlanning extend scalar model protection to twenty-two assemblies. Central typed composers preserve six Favorites Cascade FKs and four MealPlanning relationships: optional MealPlan User Cascade, MealPlanMeal Recipe Restrict, ShoppingList User Cascade, and optional ShoppingListItem Product SetNull. Same-owner mappings, indexes, converters and source provenance stay local. Central Infrastructure references Products.Domain explicitly; no schema or API change is intended.

Their PersistenceModels consume only foreign ID contracts. ScalarPersistenceBoundaryTests and the exact dependency matrix prohibit foreign Domain references; relational snapshot equivalence and PostgreSQL repository/lifecycle tests protect compatibility. Ten foreign Domain references remain across Products, Recipes and Meals PersistenceModels.

## Products

Products extends the scalar persistence boundary to twenty-three models. Its three foreign FKs live in central ProductsCrossModuleRelationships: optional ImageAsset ClientNoAction, optional UsdaFood SetNull and the unchanged conventional User relationship. The owner model uses ID-only Images.Contracts and Users.Domain.Contracts; UsdaFdcId needs no foreign contract. Central Infrastructure references Usda.Domain directly. Preserve indexes, converters, xmin and ADR 0032 image integrity; no schema or API change is intended.

Seven foreign Domain references remain in PersistenceModels: Recipes (three) and Meals (four). Existing shared database, transaction boundaries and provider behavior remain unchanged.

## Recipes and Meals

Recipes and Meals extend scalar persistence protection to twenty-five models. Their ten foreign FKs live in RecipesCrossModuleRelationships and MealsCrossModuleRelationships; all optionality and delete policies remain unchanged, including four image ClientNoAction mappings. Owned nested Recipe Restrict and Meal/Ai cascades stay local. Recognition receipts retain their User Cascade FK and deliberately have no Meal FK. The models consume direct ID contracts; existing central Domain references suffice. No module PersistenceModel retains a foreign Domain project reference.

This completes removal of foreign Domain project references from module PersistenceModels. Shared database FKs and runtime owner interactions remain; this is compile-time isolation, not database or process separation. Existing recognition-receipt migration and current main snapshot are unchanged by the boundary refactor.
