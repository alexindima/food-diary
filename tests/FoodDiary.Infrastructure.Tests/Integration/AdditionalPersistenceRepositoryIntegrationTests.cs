using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Billing;
using FoodDiary.Infrastructure.Persistence.MealPlans;
using FoodDiary.Infrastructure.Persistence.OpenFoodFacts;
using FoodDiary.Infrastructure.Persistence.RecipeComments;
using FoodDiary.Infrastructure.Persistence.RecipeLikes;
using FoodDiary.Infrastructure.Persistence.Usda;
using FoodDiary.Infrastructure.Persistence.Wearables;

namespace FoodDiary.Infrastructure.Tests.Integration;

#pragma warning disable MA0051

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AdditionalPersistenceRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [RequiresDockerFact]
    public async Task WearableRepositories_AddUpdateAndQueryConnectionsAndSyncEntries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"wearable-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var connectionRepository = new WearableConnectionRepository(context);
        var connection = WearableConnection.Create(
            user.Id,
            WearableProvider.Fitbit,
            "external-user",
            "access-token",
            "refresh-token",
            DateTime.UtcNow.AddHours(1));

        await connectionRepository.AddAsync(connection);
        await context.SaveChangesAsync();
        connection.UpdateTokens("access-token-2", "refresh-token-2", DateTime.UtcNow.AddHours(2));
        await connectionRepository.UpdateAsync(connection);
        await context.SaveChangesAsync();

        WearableConnection? savedConnection = await connectionRepository.GetAsync(user.Id, WearableProvider.Fitbit);
        IReadOnlyList<WearableConnection> allConnections = await connectionRepository.GetAllForUserAsync(user.Id);
        IReadOnlyList<WearableConnectionModel> connectionModels = await connectionRepository.GetConnectionModelsAsync(user.Id);

        Assert.NotNull(savedConnection);
        Assert.Equal("access-token-2", savedConnection.AccessToken);
        Assert.Single(allConnections);
        Assert.Equal("Fitbit", Assert.Single(connectionModels).Provider);

        var syncRepository = new WearableSyncRepository(context);
        DateTime date = DateTime.UtcNow.Date;
        var syncEntry = WearableSyncEntry.Create(user.Id, WearableProvider.Fitbit, WearableDataType.Steps, date.AddHours(9), 1200);

        await syncRepository.AddAsync(syncEntry);
        await context.SaveChangesAsync();
        syncEntry.UpdateValue(1500);
        await syncRepository.UpdateAsync(syncEntry);
        await context.SaveChangesAsync();

        WearableSyncEntry? savedEntry = await syncRepository.GetAsync(user.Id, WearableProvider.Fitbit, WearableDataType.Steps, date);
        IReadOnlyList<WearableSyncEntry> summary = await syncRepository.GetDailySummaryAsync(user.Id, date.AddHours(12));
        IReadOnlyList<WearableSyncEntryReadModel> summaryReadModels =
            await syncRepository.GetDailySummaryReadModelsAsync(user.Id, date.AddHours(12));

        Assert.NotNull(savedEntry);
        Assert.Equal(1500, savedEntry.Value);
        Assert.Single(summary);
        Assert.Equal(1500, Assert.Single(summaryReadModels).Value);
    }

    [RequiresDockerFact]
    public async Task OpenFoodFactsRepository_UpsertsSearchesAndEscapesLikePattern() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new OpenFoodFactsProductCacheRepository(context, FixedTime);
        var product = new OpenFoodFactsProductModel(
            Barcode: "123",
            Name: "100% Cocoa",
            Brand: "Brand",
            Category: "Chocolate",
            ImageUrl: "https://example.com/cocoa.png",
            CaloriesPer100G: 500,
            ProteinsPer100G: 12,
            FatsPer100G: 30,
            CarbsPer100G: 40,
            FiberPer100G: 8);

        await repository.UpsertAsync([
            product,
            product with { Name = "Duplicate ignored" },
            product with { Barcode = " ", Name = "No barcode" },
        ]);
        await context.SaveChangesAsync();
        await repository.UpsertAsync([]);
        await repository.UpsertAsync([product with { Name = "100% Cocoa Updated" }]);
        await context.SaveChangesAsync();

        IReadOnlyList<OpenFoodFactsProductModel> matches = await repository.SearchAsync("100% Cocoa", limit: 5);
        IReadOnlyList<OpenFoodFactsProductModel> blankMatches = await repository.SearchAsync("   ", limit: 5);

        OpenFoodFactsProductModel match = Assert.Single(matches);
        Assert.Equal("100% Cocoa Updated", match.Name);
        Assert.Empty(blankMatches);
    }

    [RequiresDockerFact]
    public async Task UsdaFoodRepository_ReturnsFoodsNutrientsPortionsAndReferenceValues() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        context.UsdaFoods.AddRange(
            new UsdaFood { FdcId = 1001, Description = "Apple raw" },
            new UsdaFood { FdcId = 1002, Description = "Banana raw" });
        context.UsdaNutrients.AddRange(
            new UsdaNutrient { Id = 1, Name = "Carbohydrate", UnitName = "g" },
            new UsdaNutrient { Id = 2, Name = "Protein", UnitName = "g" });
        context.UsdaFoodNutrients.AddRange(
            new UsdaFoodNutrient { Id = 1, FdcId = 1001, NutrientId = 1, Amount = 14 },
            new UsdaFoodNutrient { Id = 2, FdcId = 1001, NutrientId = 2, Amount = 0.3 });
        context.UsdaFoodPortions.Add(new UsdaFoodPortion {
            Id = 1,
            FdcId = 1001,
            Amount = 1,
            MeasureUnitName = "medium",
            GramWeight = 182,
            PortionDescription = "Medium apple",
        });
        context.DailyReferenceValues.Add(new DailyReferenceValue {
            Id = 1,
            NutrientId = 1,
            Value = 275,
            Unit = "g",
            AgeGroup = "adult",
            Gender = "all",
        });
        await context.SaveChangesAsync();

        var repository = new UsdaFoodRepository(context);

        IReadOnlyList<UsdaFood> foods = await repository.SearchAsync("apple", limit: 10);
        UsdaFood? food = await repository.GetByFdcIdAsync(1001);
        IReadOnlyList<UsdaFoodNutrient> nutrients = await repository.GetNutrientsAsync(1001);
        IReadOnlyList<UsdaFoodPortion> portions = await repository.GetPortionsAsync(1001);
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> nutrientMap = await repository.GetNutrientsByFdcIdsAsync([1001, 1002]);
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> emptyMap = await repository.GetNutrientsByFdcIdsAsync([]);
        IReadOnlyDictionary<int, DailyReferenceValue> referenceValues = await repository.GetDailyReferenceValuesAsync();
        IReadOnlyList<UsdaFoodReadModel> foodReadModels = await repository.SearchReadModelsAsync("apple", limit: 10);
        UsdaFoodReadModel? foodReadModel = await repository.GetByFdcIdReadModelAsync(1001);
        IReadOnlyList<UsdaNutrientReadModel> nutrientReadModels = await repository.GetNutrientReadModelsAsync(1001);
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientReadModelMap =
            await repository.GetNutrientReadModelsByFdcIdsAsync([1001, 1002]);
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> emptyNutrientReadModelMap =
            await repository.GetNutrientReadModelsByFdcIdsAsync([]);
        IReadOnlyList<UsdaFoodPortionModel> portionReadModels = await repository.GetPortionReadModelsAsync(1001);
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> referenceValueReadModels = await repository.GetDailyReferenceValueReadModelsAsync();

        Assert.Single(foods);
        Assert.NotNull(food);
        Assert.Equal(2, nutrients.Count);
        Assert.Equal("Carbohydrate", nutrients[0].Nutrient.Name);
        Assert.Single(portions);
        Assert.True(nutrientMap.ContainsKey(1001));
        Assert.Empty(emptyMap);
        Assert.True(referenceValues.ContainsKey(1));
        Assert.Equal(1001, Assert.Single(foodReadModels).FdcId);
        Assert.NotNull(foodReadModel);
        Assert.Equal("Apple raw", foodReadModel.Description);
        AssertUsdaNutrientReadModels(nutrientReadModels, nutrientReadModelMap);
        Assert.Empty(emptyNutrientReadModelMap);
        Assert.Equal(182, Assert.Single(portionReadModels).GramWeight);
        Assert.Equal(275, referenceValueReadModels[1].Value);
    }

    private static void AssertUsdaNutrientReadModels(
        IReadOnlyList<UsdaNutrientReadModel> nutrientReadModels,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientReadModelMap) {
        Assert.Equal("Carbohydrate", nutrientReadModels[0].Name);
        Assert.True(nutrientReadModelMap.ContainsKey(1001));
        Assert.Equal("Carbohydrate", nutrientReadModelMap[1001][0].Name);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 5, 21, 0, 0, 0, TimeSpan.Zero);
    }

    [RequiresDockerFact]
    public async Task RecipeSocialRepositories_AddQueryUpdateAndDeleteLikesAndComments() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"social-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Shared recipe", servings: 2, description: "Description");
        context.Users.Add(user);
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        var likeRepository = new RecipeLikeRepository(context);
        RecipeLike like = await likeRepository.AddAsync(RecipeLike.Create(user.Id, recipe.Id));
        await context.SaveChangesAsync();

        Assert.NotNull(await likeRepository.GetByUserAndRecipeAsync(user.Id, recipe.Id));
        Assert.True(await likeRepository.ExistsByUserAndRecipeAsync(user.Id, recipe.Id));
        Assert.Equal(1, await likeRepository.CountByRecipeAsync(recipe.Id));

        await likeRepository.DeleteAsync(like);
        await context.SaveChangesAsync();
        Assert.False(await likeRepository.ExistsByUserAndRecipeAsync(user.Id, recipe.Id));
        Assert.Equal(0, await likeRepository.CountByRecipeAsync(recipe.Id));

        var commentRepository = new RecipeCommentRepository(context);
        RecipeComment comment = await commentRepository.AddAsync(RecipeComment.Create(user.Id, recipe.Id, "First comment"));
        await context.SaveChangesAsync();
        comment.UpdateText("Updated comment");
        await commentRepository.UpdateAsync(comment);
        await context.SaveChangesAsync();

        RecipeComment? savedComment = await commentRepository.GetByIdAsync(comment.Id, asTracking: false);
        (IReadOnlyList<RecipeComment> comments, int totalComments) = await commentRepository.GetPagedByRecipeAsync(recipe.Id, page: 1, limit: 10);
        (IReadOnlyList<RecipeCommentReadModel> readModelComments, int totalReadModelComments) =
            await commentRepository.GetPagedReadModelsByRecipeAsync(recipe.Id, page: 1, limit: 10);

        Assert.NotNull(savedComment);
        Assert.Equal("Updated comment", savedComment.Text);
        Assert.Single(comments);
        Assert.Equal(1, totalComments);
        Assert.Equal("Updated comment", Assert.Single(readModelComments).Text);
        Assert.Equal(1, totalReadModelComments);

        await commentRepository.DeleteAsync(comment);
        await context.SaveChangesAsync();
        Assert.Null(await commentRepository.GetByIdAsync(comment.Id));
    }

    [RequiresDockerFact]
    public async Task MealPlanRepository_AddsAndQueriesCuratedAndUserPlans() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meal-plans-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Plan recipe", servings: 2, description: "Description");
        context.Users.Add(user);
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        var curated = MealPlan.CreateCurated("Balanced curated", "Curated", DietType.Balanced, durationDays: 7, targetCaloriesPerDay: 2000);
        curated.AddDay(1).AddMeal(MealType.Breakfast, recipe.Id, servings: 1);
        var keto = MealPlan.CreateCurated("Keto curated", "Curated", DietType.Keto, durationDays: 7, targetCaloriesPerDay: 1800);
        var userPlan = MealPlan.CreateForUser(user.Id, "User plan", description: null, DietType.Balanced, durationDays: 3, targetCaloriesPerDay: null);
        var repository = new MealPlanRepository(context);

        await repository.AddAsync(curated);
        await repository.AddAsync(keto);
        await repository.AddAsync(userPlan);
        await context.SaveChangesAsync();

        MealPlan? withDays = await repository.GetByIdAsync(curated.Id, includeDays: true);
        IReadOnlyList<MealPlan> balancedCurated = await repository.GetCuratedAsync(DietType.Balanced);
        IReadOnlyList<MealPlan> allCurated = await repository.GetCuratedAsync();
        IReadOnlyList<MealPlan> userPlans = await repository.GetByUserAsync(user.Id);
        MealPlanReadModel? readModel = await repository.GetReadModelByIdAsync(curated.Id);
        IReadOnlyList<MealPlanSummaryReadModel> balancedCuratedReadModels = await repository.GetCuratedSummaryReadModelsAsync(DietType.Balanced);
        IReadOnlyList<MealPlanSummaryReadModel> userPlanReadModels = await repository.GetByUserSummaryReadModelsAsync(user.Id);

        Assert.NotNull(withDays);
        Assert.Single(withDays.Days);
        Assert.Single(balancedCurated);
        Assert.Equal(2, allCurated.Count);
        Assert.Single(userPlans);
        Assert.NotNull(readModel);
        Assert.Equal(curated.Id.Value, readModel.Id);
        Assert.Equal("Breakfast", Assert.Single(Assert.Single(readModel.Days).Meals).MealType);
        Assert.Equal(1, Assert.Single(balancedCuratedReadModels).TotalRecipes);
        Assert.Single(userPlanReadModels);
    }

    [RequiresDockerFact]
    public async Task AdminImpersonationSessionRepository_ReturnsProjectedRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var actor = User.Create($"admin-actor-{Guid.NewGuid():N}@example.com", "hash");
        var target = User.Create($"admin-target-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(actor, target);
        await context.SaveChangesAsync();

        var sessionRepository = new AdminImpersonationSessionRepository(context);
        await sessionRepository.AddAsync(AdminImpersonationSession.Start(
            actor.Id,
            target.Id,
            "Investigating support ticket",
            "127.0.0.1",
            "UnitTest",
            DateTime.UtcNow));
        await context.SaveChangesAsync();

        (IReadOnlyList<AdminImpersonationSessionReadModel> sessions, int totalSessions) =
            await sessionRepository.GetPagedAsync(page: 0, limit: 500, search: "support");

        Assert.Single(sessions);
        Assert.Equal(1, totalSessions);
    }

    [RequiresDockerFact]
    public async Task BillingRepositories_ReturnRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var target = User.Create($"billing-target-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(target);
        await context.SaveChangesAsync();

        var subscriptionRepository = new BillingSubscriptionRepository(context);
        BillingSubscription subscription = CreateActiveSubscription(target);
        await subscriptionRepository.AddAsync(subscription);
        await context.SaveChangesAsync();

        Assert.NotNull(await subscriptionRepository.GetByUserIdAsync(target.Id));
        BillingSubscriptionOverviewReadModel? overview = await subscriptionRepository.GetOverviewReadModelByUserIdAsync(target.Id);
        Assert.NotNull(await subscriptionRepository.GetByExternalCustomerIdAsync(BillingProviderNames.Stripe, "customer-1"));
        Assert.NotNull(await subscriptionRepository.GetByExternalSubscriptionIdAsync(BillingProviderNames.Stripe, "subscription-1"));
        Assert.NotNull(await subscriptionRepository.GetByExternalPaymentMethodIdAsync(BillingProviderNames.Stripe, "payment-method-1"));
        Assert.Single(await subscriptionRepository.GetDueForRenewalAsync(BillingProviderNames.Stripe, DateTime.UtcNow.AddDays(30), limit: 10));
        Assert.NotNull(overview);
        BillingSubscriptionOverviewReadModel overviewValue = overview;
        Assert.Equal(target.Id.Value, overviewValue.UserId);
        Assert.Equal(BillingProviderNames.Stripe, overviewValue.Provider);
        Assert.Equal("active", overviewValue.Status);

        var paymentRepository = new BillingPaymentRepository(context);
        BillingPayment payment = await paymentRepository.AddAsync(CreatePayment(target, subscription.Id));
        await context.SaveChangesAsync();

        Assert.Same(payment, await paymentRepository.GetByExternalPaymentIdAsync(BillingProviderNames.Stripe, "payment-1"));

        var webhookRepository = new BillingWebhookEventRepository(context);
        await webhookRepository.AddAsync(CreateWebhookEvent());
        await context.SaveChangesAsync();

        Assert.True(await webhookRepository.ExistsAsync(BillingProviderNames.Stripe, "event-1"));
    }

    [RequiresDockerFact]
    public async Task AdminBillingRepository_ReturnsProjectedRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var target = User.Create($"admin-billing-target-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(target);
        await context.SaveChangesAsync();

        BillingSubscription subscription = CreateActiveSubscription(target);
        context.BillingSubscriptions.Add(subscription);
        context.BillingPayments.Add(CreatePayment(target, subscription.Id));
        BillingWebhookEvent webhookEvent = CreateWebhookEvent();
        context.BillingWebhookEvents.Add(webhookEvent);
        await context.SaveChangesAsync();

        var adminBillingRepository = new AdminBillingRepository(context);
        AdminBillingListFilter filter = new(
            Page: 1,
            Limit: 20,
            Provider: BillingProviderNames.Stripe,
            Status: null,
            Kind: null,
            Search: "customer-1",
            FromUtc: DateTime.UtcNow.AddDays(-1),
            ToUtc: DateTime.UtcNow.AddDays(1));

        Assert.Single((await adminBillingRepository.GetSubscriptionsAsync(filter)).Items);
        Assert.Single((await adminBillingRepository.GetSubscriptionsAsync(filter with { Status = "active" })).Items);
        Assert.Single((await adminBillingRepository.GetPaymentsAsync(filter with { Status = "succeeded", Kind = "subscription" })).Items);
        Assert.Single((await adminBillingRepository.GetWebhookEventsAsync(filter with { Status = "processed", Search = webhookEvent.EventId })).Items);
    }

    private static BillingSubscription CreateActiveSubscription(User user) {
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Stripe,
            "customer-1",
            "price-monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            "subscription-1",
            "payment-method-1",
            "price-monthly",
            "monthly",
            "active",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            webhookEventId: "event-subscription",
            syncedAtUtc: DateTime.UtcNow,
            providerMetadataJson: "{}");
        return subscription;
    }

    private static BillingPayment CreatePayment(User user, Guid subscriptionId) {
        return BillingPayment.Create(
            user.Id,
            subscriptionId,
            BillingProviderNames.Stripe,
            "payment-1",
            "customer-1",
            "subscription-1",
            "payment-method-1",
            "price-monthly",
            "monthly",
            "succeeded",
            "subscription",
            199m,
            "USD",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            "event-payment",
            "{}");
    }

    private static BillingWebhookEvent CreateWebhookEvent() {
        return BillingWebhookEvent.CreateProcessed(
            BillingProviderNames.Stripe,
            "event-1",
            "invoice.paid",
            "payment-1",
            DateTime.UtcNow,
            "{}");
    }

    [RequiresDockerFact]
    public async Task EmailTemplateRepository_GetsOrdersAndUpsertsTemplates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new EmailTemplateRepository(context);

        await repository.UpsertAsync("welcome", "en", "Welcome", "<p>Hello</p>", "Hello", isActive: true);
        await context.SaveChangesAsync();
        await repository.UpsertAsync("welcome", "en", "Welcome back", "<p>Hi</p>", "Hi", isActive: false);
        await context.SaveChangesAsync();
        await repository.UpsertAsync("reset", "ru", "Reset", "<p>Reset</p>", "Reset", isActive: true);
        await context.SaveChangesAsync();

        IReadOnlyList<FoodDiary.Domain.Entities.Content.EmailTemplate> templates = await repository.GetAllAsync();
        IReadOnlyList<EmailTemplateReadModel> templateReadModels = await repository.GetAllReadModelsAsync();
        FoodDiary.Domain.Entities.Content.EmailTemplate? template = await repository.GetByKeyAsync("welcome", "en");

        Assert.Contains(
            templates,
            item => string.Equals(item.Key, "reset", StringComparison.Ordinal) &&
                    string.Equals(item.Locale, "ru", StringComparison.Ordinal));
        Assert.Contains(
            templates,
            item => string.Equals(item.Key, "welcome", StringComparison.Ordinal) &&
                    string.Equals(item.Locale, "en", StringComparison.Ordinal));
        Assert.NotNull(template);
        Assert.Contains(templateReadModels, item => string.Equals(item.Subject, "Welcome back", StringComparison.Ordinal));
        Assert.Equal("Welcome back", template.Subject);
        Assert.False(template.IsActive);
    }
}
