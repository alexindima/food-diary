using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Billing;
using FoodDiary.Infrastructure.Persistence.Content;
using FoodDiary.Infrastructure.Persistence.ContentReports;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.FavoriteMeals;
using FoodDiary.Infrastructure.Persistence.FavoriteProducts;
using FoodDiary.Infrastructure.Persistence.FavoriteRecipes;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Meals;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Infrastructure.Persistence.ShoppingLists;
using FoodDiary.Infrastructure.Persistence.Tracking;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace FoodDiary.Infrastructure.Tests.Integration;

#pragma warning disable MA0004

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class PersistenceRepositoryCoverageIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime FixedNow = new(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedDateTimeProvider(FixedNow);

    [RequiresDockerFact]
    public async Task NutritionLessonRepository_CoversFilteredTrackingProgressAndDeletePaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"lesson-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new NutritionLessonRepository(context);
        var basics = NutritionLesson.Create(
            "Basics",
            "Content",
            "Summary",
            "EN",
            LessonCategory.NutritionBasics,
            LessonDifficulty.Beginner,
            estimatedReadMinutes: 0,
            sortOrder: -1);
        var hydration = NutritionLesson.Create(
            "Hydration",
            "Drink water",
            summary: null,
            "en",
            LessonCategory.Hydration,
            LessonDifficulty.Intermediate,
            estimatedReadMinutes: 3,
            sortOrder: 2);

        await repository.AddRangeAsync([hydration]);
        await repository.AddAsync(basics);
        await context.SaveChangesAsync();

        IReadOnlyList<NutritionLesson> nutritionLessons =
            await repository.GetByLocaleAsync("en", LessonCategory.NutritionBasics);
        IReadOnlyList<NutritionLesson> allLessons = await repository.GetAllAsync();
        NutritionLesson? saved = await repository.GetByIdAsync(basics.Id);
        NutritionLesson? tracked = await repository.GetByIdTrackingAsync(basics.Id);
        Assert.NotNull(tracked);
        tracked.Update("Basics updated", "Updated content", "Updated", "en", LessonCategory.NutritionBasics, LessonDifficulty.Advanced, 5, 3);
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        UserLessonProgress progress = await repository.AddProgressAsync(
            UserLessonProgress.Create(user.Id, basics.Id, DateTime.UtcNow));
        await context.SaveChangesAsync();
        IReadOnlyList<UserLessonProgress> allProgress = await repository.GetUserProgressAsync(user.Id);
        UserLessonProgress? lessonProgress = await repository.GetUserProgressForLessonAsync(user.Id, basics.Id);

        await repository.DeleteAsync(hydration);
        await context.SaveChangesAsync();

        Assert.Single(nutritionLessons);
        Assert.Equal(2, allLessons.Count);
        Assert.NotNull(saved);
        Assert.Equal("Basics updated", tracked.Title);
        Assert.Equal(progress.Id, Assert.Single(allProgress).Id);
        Assert.Equal(progress.Id, lessonProgress?.Id);
        Assert.Null(await repository.GetByIdAsync(hydration.Id));
    }

    [RequiresDockerFact]
    public async Task ContentReportRepository_CoversStatusFiltersAndTrackingUpdatePaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"report-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new ContentReportRepository(context);
        var targetId = Guid.NewGuid();
        ContentReport report = await repository.AddAsync(ContentReport.Create(user.Id, ReportTargetType.Recipe, targetId, "Spam"));
        ContentReport otherReport = await repository.AddAsync(ContentReport.Create(user.Id, ReportTargetType.Comment, Guid.NewGuid(), "Abuse"));
        await context.SaveChangesAsync();

        ContentReport? tracked = await repository.GetByIdAsync(report.Id, asTracking: true);
        Assert.NotNull(tracked);
        tracked.MarkDismissed("Not actionable");
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        bool hasReported = await repository.HasUserReportedAsync(user.Id, ReportTargetType.Recipe, targetId);
        (IReadOnlyList<ContentReport> pendingItems, int pendingTotal) =
            await repository.GetPagedAsync(ReportStatus.Pending, page: 1, limit: 10);
        (IReadOnlyList<ContentReport> allItems, int allTotal) =
            await repository.GetPagedAsync(status: null, page: 1, limit: 1);
        int dismissedCount = await repository.CountByStatusAsync(ReportStatus.Dismissed);

        Assert.True(hasReported);
        Assert.Equal(otherReport.Id, Assert.Single(pendingItems).Id);
        Assert.Equal(1, pendingTotal);
        Assert.Single(allItems);
        Assert.Equal(2, allTotal);
        Assert.Equal(1, dismissedCount);
    }

    [RequiresDockerFact]
    public async Task ShoppingListRepository_CoversIncludeTrackingUpdateAndDeletePaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"shopping-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"shopping-other-{Guid.NewGuid():N}@example.com", "hash");
        Product product = CreateProduct(user.Id, "Oats");
        context.Users.AddRange(user, otherUser);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var repository = new ShoppingListRepository(context);
        var list = ShoppingList.Create(user.Id, "Weekly");
        ShoppingListItem item = AddShoppingListItemWithSource(
            list,
            product.Id);
        await repository.AddAsync(list);
        await context.SaveChangesAsync();

        ShoppingList? byId = await repository.GetByIdAsync(list.Id, user.Id, includeItems: true);
        ShoppingList? current = await repository.GetCurrentAsync(user.Id, includeItems: true, asTracking: true);
        IReadOnlyList<ShoppingList> allLists = await repository.GetAllAsync(user.Id, includeItems: true);
        await AssertShoppingListReadModelsAsync(repository, list.Id, user.Id, otherUser.Id);
        Assert.NotNull(current);
        current.UpdateName("Weekly updated");
        current.FindItem(item.Id)?.UpdateDetails(
            "Rolled oats",
            product.Id,
            750,
            MeasurementUnit.G,
            "Breakfast",
            "A2",
            note: null,
            isChecked: true,
            checkedOnUtc: DateTime.UtcNow,
            sortOrder: 2);
        await repository.UpdateAsync(current);
        await context.SaveChangesAsync();

        await repository.DeleteAsync(current);
        await context.SaveChangesAsync();
        await repository.DeleteAsync(current);
        await context.SaveChangesAsync();

        Assert.Contains(byId!.Items, savedItem => savedItem.Id == item.Id);
        Assert.Equal(2, byId.Items.Count);
        Assert.Single(allLists);
        Assert.Null(await repository.GetByIdAsync(list.Id, user.Id));
    }

    private static ShoppingListItem AddShoppingListItemWithSource(ShoppingList list, ProductId productId) {
        ShoppingListItem item = list.AddItem(
            "Oats",
            productId,
            500,
            MeasurementUnit.G,
            "Pantry",
            isChecked: false,
            sortOrder: 1,
            aisle: "A1",
            note: "Organic");
        item.AddMealPlanSource(
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            "Day 1 breakfast",
            dayNumber: 1,
            mealType: "Breakfast",
            amount: 500,
            unit: MeasurementUnit.G);
        list.AddItem(
            "Loose note",
            productId: null,
            amount: null,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 2);
        return item;
    }

    private static async Task AssertShoppingListReadModelsAsync(
        ShoppingListRepository repository,
        ShoppingListId listId,
        UserId userId,
        UserId otherUserId) {
        ShoppingListReadModel? byIdReadModel = await repository.GetReadModelByIdAsync(listId, userId);
        ShoppingListReadModel? forbiddenReadModel = await repository.GetReadModelByIdAsync(listId, otherUserId);
        ShoppingListReadModel? currentReadModel = await repository.GetCurrentReadModelAsync(userId);
        IReadOnlyList<ShoppingListSummaryReadModel> summaries = await repository.GetAllSummaryReadModelsAsync(userId);

        Assert.NotNull(byIdReadModel);
        ShoppingListItemReadModel oats = Assert.Single(
            byIdReadModel.Items,
            item => string.Equals(item.Name, "Oats", StringComparison.Ordinal));
        Assert.Null(Assert.Single(
            byIdReadModel.Items,
            item => string.Equals(item.Name, "Loose note", StringComparison.Ordinal)).ProductId);
        Assert.Equal("Day 1 breakfast", Assert.Single(oats.Sources).Label);
        Assert.Null(forbiddenReadModel);
        Assert.Equal(listId.Value, currentReadModel?.Id);
        Assert.Equal(2, Assert.Single(summaries).ItemsCount);
    }

    [RequiresDockerFact]
    public async Task FavoriteRepositories_CoverEmptyLookupsIncludesUpdatesAndDeletes() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"favorite-{Guid.NewGuid():N}@example.com", "hash");
        Product product = CreateProduct(user.Id, "Rice");
        var recipe = Recipe.Create(user.Id, "Pilaf", servings: 2, description: "Rice dish");
        var meal = Meal.Create(user.Id, DateTime.UtcNow, MealType.Dinner);
        meal.AddProduct(product.Id, 120);
        context.Users.Add(user);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        await CoverFavoriteProductRepositoryAsync(context, user.Id, product.Id);
        await CoverFavoriteRecipeRepositoryAsync(context, user.Id, recipe.Id);
        await CoverFavoriteMealRepositoryAsync(context, user.Id, meal.Id);
    }

    [RequiresDockerFact]
    public async Task TrackingRepositories_CoverDateFiltersTotalsAndFastingQueries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"tracking-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        DateTime today = DateTime.UtcNow.Date;
        var hydrationRepository = new HydrationEntryRepository(context);
        HydrationEntry hydration = await hydrationRepository.AddAsync(HydrationEntry.Create(user.Id, today.AddHours(8), 250));
        await hydrationRepository.AddAsync(HydrationEntry.Create(user.Id, today.AddHours(10), 500));
        await context.SaveChangesAsync();
        hydration.Update(300, today.AddHours(9));
        await hydrationRepository.UpdateAsync(hydration);
        await context.SaveChangesAsync();
        Assert.NotNull(await hydrationRepository.GetByIdAsync(hydration.Id));
        Assert.NotNull(await hydrationRepository.GetByIdAsync(hydration.Id, asTracking: true));
        Assert.Equal(2, (await hydrationRepository.GetByDateAsync(user.Id, today)).Count);
        Assert.Equal(800, await hydrationRepository.GetDailyTotalAsync(user.Id, today));
        Assert.Single(await hydrationRepository.GetDailyTotalsAsync(user.Id, today.AddDays(-1), today.AddDays(1)));

        (WeightEntry weight, WaistEntry waist) = await CoverBodyMetricRepositoriesAsync(context, user.Id, today);

        var exerciseRepository = new ExerciseEntryRepository(context);
        ExerciseEntry exercise = await exerciseRepository.AddAsync(ExerciseEntry.Create(user.Id, today, ExerciseType.Cardio, 45, 300, "Run", "Easy"));
        await context.SaveChangesAsync();
        exercise.Update(caloriesBurned: 320, clearNotes: true);
        await exerciseRepository.UpdateAsync(exercise);
        await context.SaveChangesAsync();
        Assert.NotNull(await exerciseRepository.GetByIdAsync(exercise.Id, user.Id, asTracking: true));
        Assert.Single(await exerciseRepository.GetByDateRangeAsync(user.Id, today.AddDays(-1), today.AddDays(1)));
        ExerciseEntryReadModel exerciseReadModel = Assert.Single(await exerciseRepository.GetByDateRangeReadModelsAsync(user.Id, today.AddDays(-1), today.AddDays(1)));
        Assert.Equal(exercise.Id.Value, exerciseReadModel.Id);
        Assert.Equal("Cardio", exerciseReadModel.ExerciseType);
        Assert.Equal(320, exerciseReadModel.CaloriesBurned);
        Assert.Equal(320, await exerciseRepository.GetTotalCaloriesBurnedAsync(user.Id, today));

        await hydrationRepository.DeleteAsync(hydration);
        await new WeightEntryRepository(context).DeleteAsync(weight);
        await new WaistEntryRepository(context).DeleteAsync(waist);
        await exerciseRepository.DeleteAsync(exercise);
        await context.SaveChangesAsync();
    }

    private static async Task<(WeightEntry Weight, WaistEntry Waist)> CoverBodyMetricRepositoriesAsync(
        FoodDiaryDbContext context,
        UserId userId,
        DateTime today) {
        var weightRepository = new WeightEntryRepository(context);
        WeightEntry weight = await weightRepository.AddAsync(WeightEntry.Create(userId, today, 80));
        await context.SaveChangesAsync();
        weight.Update(79.5, today.AddDays(-1));
        await weightRepository.UpdateAsync(weight);
        await context.SaveChangesAsync();
        Assert.NotNull(await weightRepository.GetByIdAsync(weight.Id, userId));
        Assert.NotNull(await weightRepository.GetByIdAsync(weight.Id, userId, asTracking: true));
        Assert.NotNull(await weightRepository.GetByDateAsync(userId, today.AddDays(-1)));
        Assert.Single(await weightRepository.GetEntriesAsync(userId, today.AddDays(-2), today, limit: 1, descending: true));
        Assert.NotEmpty(await weightRepository.GetEntriesAsync(userId, dateFrom: null, dateTo: null, limit: null, descending: false));
        Assert.Single(await weightRepository.GetByPeriodAsync(userId, today.AddDays(-2), today));

        var waistRepository = new WaistEntryRepository(context);
        WaistEntry waist = await waistRepository.AddAsync(WaistEntry.Create(userId, today, 90));
        await context.SaveChangesAsync();
        waist.Update(89.5, today.AddDays(-1));
        await waistRepository.UpdateAsync(waist);
        await context.SaveChangesAsync();
        Assert.NotNull(await waistRepository.GetByIdAsync(waist.Id, userId));
        Assert.NotNull(await waistRepository.GetByIdAsync(waist.Id, userId, asTracking: true));
        Assert.NotNull(await waistRepository.GetByDateAsync(userId, today.AddDays(-1)));
        Assert.Single(await waistRepository.GetEntriesAsync(userId, today.AddDays(-2), today, limit: 1, descending: false));
        Assert.NotEmpty(await waistRepository.GetEntriesAsync(userId, dateFrom: null, dateTo: null, limit: null, descending: true));
        Assert.Single(await waistRepository.GetByPeriodAsync(userId, today.AddDays(-2), today));

        return (weight, waist);
    }

    [RequiresDockerFact]
    public async Task FastingRepositories_CoverPlanOccurrenceSessionCheckInAndTelemetryQueries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"fasting-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        DateTime now = DateTime.UtcNow;
        FastingPlanRepository planRepository = new(context);
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, now.AddDays(-3), "Plan");
        await planRepository.AddAsync(plan);
        await context.SaveChangesAsync();
        plan.Pause();
        await planRepository.UpdateAsync(plan);
        await context.SaveChangesAsync();
        plan.Resume();
        await planRepository.UpdateAsync(plan);
        await context.SaveChangesAsync();
        Assert.NotNull(await planRepository.GetActiveAsync(user.Id, asTracking: true));
        Assert.NotNull(await planRepository.GetByIdAsync(plan.Id, asTracking: true));
        Assert.Single(await planRepository.GetByUserAsync(user.Id, FastingPlanType.Intermittent, FastingPlanStatus.Active));

        await CoverFastingOccurrenceAndCheckInsAsync(context, user.Id, plan.Id, now);
        FastingSession currentSession = await CoverFastingSessionsAsync(context, user.Id, FixedNow);
        await CoverFastingTelemetryAsync(context, currentSession.Id, FixedNow);
    }

    [RequiresDockerFact]
    public async Task CycleRepository_CoversDetailsTrackingCurrentAndUserQueries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"cycle-repo-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        DateTime today = DateTime.UtcNow.Date;
        var repository = new CycleRepository(context);
        var profile = CycleProfile.Create(
            user.Id,
            today.AddDays(-28),
            CycleTrackingMode.TryingToConceive,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 13,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: false,
            notes: "Initial");
        AddCycleDetails(profile, today);
        await repository.AddAsync(profile);
        await context.SaveChangesAsync();

        CycleProfile? tracked = await repository.GetByIdAsync(profile.Id, user.Id, includeDetails: true, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateSettings(new CycleProfileSettings(
            tracked.Mode,
            tracked.AverageCycleLength,
            tracked.AveragePeriodLength,
            tracked.LutealLength,
            tracked.IsRegular,
            tracked.IsOnboardingComplete,
            tracked.ShowFertilityEstimates,
            tracked.DiscreetNotifications,
            Notes: "Updated"));
        tracked.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 5, ["mild"], "updated");
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        CycleProfile? byId = await repository.GetByIdAsync(profile.Id, user.Id, includeDetails: true);
        CycleProfile? current = await repository.GetCurrentAsync(user.Id, includeDetails: true);
        IReadOnlyList<CycleProfile> profiles = await repository.GetByUserAsync(user.Id, includeDetails: true);

        Assert.Equal("Updated", byId?.Notes);
        Assert.NotEmpty(byId!.BleedingEntries);
        Assert.NotEmpty(byId.SymptomEntries);
        Assert.NotEmpty(byId.Factors);
        Assert.NotEmpty(byId.FertilitySignals);
        Assert.Equal(profile.Id, current?.Id);
        Assert.Equal(profile.Id, Assert.Single(profiles).Id);
    }

    [RequiresDockerFact]
    public async Task DailyAdviceRepository_CoversLocaleNormalizationPaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        context.DailyAdvices.Add(DailyAdvice.Create("Drink water", "en", tag: "hydration"));
        context.DailyAdvices.Add(DailyAdvice.Create("Ru advice", "ru", tag: "hydration"));
        await context.SaveChangesAsync();

        var repository = new DailyAdviceRepository(context);

        Assert.Contains(await repository.GetByLocaleAsync(" "), advice => string.Equals(advice.Value, "Drink water", StringComparison.Ordinal));
        Assert.Contains(await repository.GetByLocaleAsync("RU-ru"), advice => string.Equals(advice.Value, "Ru advice", StringComparison.Ordinal));
    }

    [RequiresDockerFact]
    public async Task NotificationRepositories_CoverReadExpiryAndWebPushDeletePaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"notifications-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await CoverNotificationRepositoryAsync(context, user.Id);
        await CoverWebPushSubscriptionRepositoryAsync(context, user.Id);
    }

    [RequiresDockerFact]
    public async Task BillingRecommendationAndDietologistRepositories_CoverFilteredAndDuplicatePaths() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var client = User.Create($"client-{Guid.NewGuid():N}@example.com", "hash");
        var dietologist = User.Create($"dietologist-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(client, dietologist);
        await context.SaveChangesAsync();

        await CoverBillingRepositoriesAsync(context, client.Id);
        await CoverRecommendationRepositoryAsync(context, dietologist.Id, client.Id);
        await CoverDietologistInvitationRepositoryAsync(context, dietologist.Id, client.Id);
    }

    [RequiresDockerFact]
    public async Task RecipeRepository_CoversSearchIncludesUsageNutritionExploreAndDeleteBranches() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"recipe-owner-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"recipe-other-{Guid.NewGuid():N}@example.com", "hash");
        Product product = CreateProduct(owner.Id, "Flour");
        context.Users.AddRange(owner, other);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var repository = new RecipeRepository(context);
        var publicRecipe = Recipe.Create(owner.Id, "100% Pancake", servings: 2, description: "Breakfast_Recipe", category: "Breakfast", prepTime: 15);
        publicRecipe.AddStep(stepNumber: 1, instruction: "Mix ingredients");
        publicRecipe.ApplyComputedNutrition(200, 8, 4, 30, 3, 0);
        var privateRecipe = Recipe.Create(owner.Id, "Private soup", servings: 1, visibility: Visibility.Private);
        var otherPublic = Recipe.Create(other.Id, "Public salad", servings: 1, category: "Salad", prepTime: 5);
        await repository.AddAsync(publicRecipe);
        await repository.AddAsync(privateRecipe);
        await repository.AddAsync(otherPublic);
        await context.SaveChangesAsync();

        var meal = Meal.Create(owner.Id, DateTime.UtcNow, MealType.Lunch);
        meal.AddRecipe(publicRecipe.Id, servings: 1);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        await AssertRecipeRepositoryQueriesAsync(context, repository, owner.Id, publicRecipe, privateRecipe, otherPublic);
    }

    [RequiresDockerFact]
    public async Task UserRepositories_CoverStatusFiltersDashboardLoginCleanupAndRefreshSessions() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create($"active-{Guid.NewGuid():N}@example.com", "hash");
        active.LinkTelegram(123456);
        var inactive = User.Create($"inactive-{Guid.NewGuid():N}@example.com", "hash");
        inactive.Deactivate(DateTime.UtcNow);
        var deleted = User.Create($"deleted-{Guid.NewGuid():N}@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        Role premiumRole = await context.Roles.FirstOrDefaultAsync(role => role.Name == RoleNames.Premium) ?? Role.Create(RoleNames.Premium);
        context.Users.AddRange(active, inactive, deleted);
        if (context.Entry(premiumRole).State == EntityState.Detached) {
            context.Roles.Add(premiumRole);
        }
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);
        var roleCatalogService = new UserRoleCatalogService(context);
        var added = User.Create($"added-{Guid.NewGuid():N}@example.com", "hash");
        await repository.AddAsync(added);
        await context.SaveChangesAsync();
        added.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "ru"));
        await repository.UpdateAsync(added);
        await context.SaveChangesAsync();

        await AssertUserLookupRepositoryAsync(repository, active, deleted);
        Assert.Empty((await repository.GetPagedAsync("missing", page: 1, limit: 10, UserAccountStatusFilter.Active)).Items);
        Assert.Empty((await repository.GetPagedAsync("%", page: 0, limit: 0, includeDeleted: false)).Items);
        Assert.Single((await repository.GetPagedAsync("inactive", page: 1, limit: 10, UserAccountStatusFilter.Inactive)).Items);
        Assert.Single((await repository.GetPagedAsync("deleted", page: 1, limit: 10, includeDeleted: true)).Items);
        Assert.Single((await repository.GetPagedAsync("deleted", page: 1, limit: 10, UserAccountStatusFilter.Deleted)).Items);
        Assert.Equal(4, (await repository.GetPagedAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.All)).TotalItems);
        Assert.Equal(4, (await repository.GetAdminDashboardSummaryAsync(recentLimit: 2)).TotalUsers);
        await AssertUserAdminReadModelsAsync(repository, active);
        Assert.Contains(await roleCatalogService.GetRolesByNamesAsync([RoleNames.Premium]), role => string.Equals(role.Name, RoleNames.Premium, StringComparison.Ordinal));

        var auditEvent = UserRoleAuditEvent.Create(active.Id, premiumRole, UserRoleAuditAction.Added, actorUserId: null, "tests", DateTime.UtcNow);
        active.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "ru"));
        await repository.UpdateAsync(active, [auditEvent]);
        await context.SaveChangesAsync();

        await CoverUserLoginEventRepositoryAsync(context, active.Id);
        await CoverRefreshTokenSessionRepositoryAsync(context, active.Id);
    }

    private static async Task AssertUserLookupRepositoryAsync(UserRepository repository, User active, User deleted) {
        Assert.NotNull(await repository.GetByEmailAsync(active.Email));
        Assert.NotNull(await repository.GetByEmailIncludingDeletedAsync(deleted.Email));
        Assert.NotNull(await repository.GetByIdAsync(active.Id));
        Assert.NotNull(await repository.GetByIdIncludingDeletedAsync(deleted.Id));
        Assert.NotNull(await repository.GetByTelegramUserIdAsync(123456));
        Assert.NotNull(await repository.GetByTelegramUserIdIncludingDeletedAsync(123456));
    }

    private static async Task AssertUserAdminReadModelsAsync(UserRepository repository, User active) {
        Assert.Equal(active.Email, (await repository.GetByIdIncludingDeletedReadModelAsync(active.Id))?.Email);
        Assert.Single((await repository.GetPagedReadModelsAsync("inactive", page: 1, limit: 10, UserAccountStatusFilter.Inactive)).Items);
        Assert.Equal(4, (await repository.GetAdminDashboardSummaryReadModelsAsync(recentLimit: 2)).TotalUsers);
    }

    [RequiresDockerFact]
    public async Task ProductMealRecentImageAdminAndBillingRepositories_CoverRemainingPersistenceBranches() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"persistence-user-{Guid.NewGuid():N}@example.com", "hash");
        var actor = User.Create($"persistence-actor-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"persistence-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(user, actor, otherUser);
        await context.SaveChangesAsync();

        await CoverProductAndCachedProductRepositoriesAsync(context, user.Id, otherUser.Id);
        await CoverMealRepositoryAsync(context, user.Id);
        await CoverRecentItemRepositoryAsync(context, user.Id);
        await CoverImageAssetRepositoryAsync(context, user.Id);
        await CoverAdminUserRoleAuditRepositoryAsync(context, user.Id, actor.Id);
        await CoverBillingTransactionRunnerAsync(context);
    }

#pragma warning disable MA0004
    private static async Task CoverProductAndCachedProductRepositoriesAsync(
        FoodDiaryDbContext context,
        UserId userId,
        UserId otherUserId) {
        var repository = new ProductRepository(context);
        var searchable = Product.Create(
            userId,
            "100% Milk_One",
            MeasurementUnit.Ml,
            100,
            250,
            65,
            3,
            2,
            5,
            0,
            0,
            barcode: "milk_100",
            brand: "Brand%",
            productType: ProductType.Dairy,
            category: "Dairy",
            visibility: Visibility.Public);
        var privateProduct = Product.Create(userId, "Private oats", MeasurementUnit.G, 100, 50, 380, 10, 7, 60, 8, 0, visibility: Visibility.Private);
        var otherPublic = Product.Create(otherUserId, "Other public", MeasurementUnit.G, 100, 100, 120, 1, 1, 20, 1, 0);

        await repository.AddAsync(searchable);
        await repository.AddAsync(privateProduct);
        await repository.AddAsync(otherPublic);
        await context.SaveChangesAsync();

        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Breakfast);
        meal.AddProduct(searchable.Id, 200);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var productOverviewReadService = new ProductOverviewReadService(context);
        (IReadOnlyList<ProductOverviewReadItem> searchedItems, int searchedTotal) =
            await productOverviewReadService.GetPagedAsync(userId, includePublic: true, page: 0, limit: 0, filters: new ProductQueryFilters("100% Milk_", [ProductType.Dairy]));
        Product? publicById = await repository.GetByIdAsync(otherPublic.Id, userId);
        Product? privateByIdWithoutPublic = await repository.GetByIdAsync(privateProduct.Id, userId, includePublic: false);
        Product? tracked = await repository.GetByIdForUpdateAsync(searchable.Id, userId, includePublic: false);
        Assert.NotNull(tracked);
        tracked.UpdateCoreIdentity(name: "Updated milk");
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        IReadOnlyDictionary<ProductId, Product> emptyByIds = await repository.GetByIdsAsync([], userId);
        IReadOnlyDictionary<ProductId, Product> byIds = await repository.GetByIdsAsync([searchable.Id, otherPublic.Id, otherPublic.Id], userId);
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> emptyUsage = await productOverviewReadService.GetByIdsWithUsageAsync([], userId);
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> usage = await productOverviewReadService.GetByIdsWithUsageAsync([searchable.Id], userId);

        await CoverCachedProductRepositoryAsync(repository, searchable.Id, userId);
        await DeleteExistingProductAsync(context, repository, privateProduct.Id, userId);

        Assert.Single(searchedItems);
        Assert.Equal(1, searchedTotal);
        Assert.NotNull(publicById);
        Assert.NotNull(privateByIdWithoutPublic);
        Assert.Empty(emptyByIds);
        Assert.Equal(2, byIds.Count);
        Assert.Empty(emptyUsage);
        Assert.Equal(1, usage[searchable.Id].UsageCount);
    }

    private static async Task DeleteExistingProductAsync(
        FoodDiaryDbContext context,
        ProductRepository repository,
        ProductId productId,
        UserId userId) {
        context.ChangeTracker.Clear();
        Product? product = await repository.GetByIdForUpdateAsync(productId, userId, includePublic: false);
        Assert.NotNull(product);
        await repository.DeleteAsync(product);
        await context.SaveChangesAsync();
        await repository.DeleteAsync(product);
    }

    private static async Task CoverCachedProductRepositoryAsync(
        ProductRepository repository,
        ProductId productId,
        UserId userId) {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cachedRepository = new CachedProductRepository(repository, cache);
        Assert.NotNull(await cachedRepository.GetByIdAsync(productId, userId));
        Assert.NotNull(await cachedRepository.GetByIdAsync(productId, userId));
        Assert.NotNull(await cachedRepository.GetByIdForUpdateAsync(productId, userId, includePublic: false));
        Assert.Single(await cachedRepository.GetByIdsAsync([productId], userId));

        var cacheAdded = Product.Create(userId, "Cache add", MeasurementUnit.G, 100, 100, 1, 1, 1, 1, 1, 0);
        await cachedRepository.AddAsync(cacheAdded);
        cacheAdded.UpdateCoreIdentity(name: "Cache updated");
        await cachedRepository.UpdateAsync(cacheAdded);
        await cachedRepository.DeleteAsync(cacheAdded);
    }

    private static async Task CoverMealRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new MealRepository(context);
        DateTime now = DateTime.UtcNow;
        var meal = Meal.Create(userId, DateTime.SpecifyKind(now.Date.AddHours(8), DateTimeKind.Unspecified), MealType.Breakfast, "Start");
        await repository.AddAsync(meal);
        await context.SaveChangesAsync();

        meal.UpdateComment("Updated");
        await repository.UpdateAsync(meal);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByIdAsync(meal.Id, userId));
        Assert.NotNull(await repository.GetByIdAsync(meal.Id, userId, includeItems: true, asTracking: true));
        Assert.NotEmpty((await repository.GetPagedAsync(userId, page: 0, limit: 0, filters: new MealQueryFilters(now.Date.ToLocalTime(), now.Date))).Items);
        Assert.NotEmpty((await repository.GetPagedAsync(userId, page: 1, limit: 10, filters: new MealQueryFilters(DateFrom: DateTime.SpecifyKind(now.Date, DateTimeKind.Local), DateTo: null))).Items);
        Assert.NotEmpty((await repository.GetPagedAsync(userId, page: 1, limit: 10, filters: new MealQueryFilters(DateFrom: null, DateTo: now.Date.AddHours(23)))).Items);
        Assert.NotEmpty((await repository.GetPagedAsync(userId, page: 1, limit: 10, filters: new MealQueryFilters(DateFrom: DateTime.SpecifyKind(now.Date, DateTimeKind.Unspecified), DateTo: null))).Items);
        Assert.NotEmpty(await repository.GetByPeriodAsync(userId, now.Date.AddDays(-1), now.Date.AddDays(1)));
        Assert.NotEmpty(await repository.GetDistinctMealDatesAsync(userId, now.Date.AddDays(-1), now.Date.AddDays(1)));
        Assert.True(await repository.GetTotalMealCountAsync(userId) >= 1);
        Assert.NotNull(await repository.GetWithItemsAndProductsAsync(userId, now.Date));

        await repository.DeleteAsync(meal);
        await context.SaveChangesAsync();
        await repository.DeleteAsync(meal);
    }

    private static async Task CoverRecentItemRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        DateTime baseline = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);
        var repository = new RecentItemRepository(context, new FixedDateTimeProvider(baseline));
        var productId = ProductId.New();
        var recipeId = RecipeId.New();

        await repository.RegisterUsageAsync(userId, [], []);
        await repository.RegisterUsageAsync(userId, [productId, productId], [recipeId]);
        await context.SaveChangesAsync();
        await repository.RegisterUsageAsync(userId, [productId], [recipeId]);
        await context.SaveChangesAsync();

        IReadOnlyList<RecentProductUsage> products = await repository.GetRecentProductsAsync(userId, limit: 0);
        IReadOnlyList<RecentRecipeUsage> recipes = await repository.GetRecentRecipesAsync(userId, limit: 500);

        Assert.Equal(2, Assert.Single(products).UsageCount);
        Assert.Equal(2, Assert.Single(recipes).UsageCount);
    }

    private static async Task CoverImageAssetRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new ImageAssetRepository(context);
        ImageAsset asset = await repository.AddAsync(ImageAsset.Create(userId, $"images/{Guid.NewGuid():N}.webp", "https://cdn.example.com/image.webp"));
        ImageAsset usedAsset = await repository.AddAsync(ImageAsset.Create(userId, $"images/{Guid.NewGuid():N}.webp", "https://cdn.example.com/used.webp"));
        await context.SaveChangesAsync();
        var product = Product.Create(userId, "Product with image", MeasurementUnit.G, 100, 100, 10, 1, 1, 1, 1, 0, imageAssetId: usedAsset.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByIdAsync(asset.Id));
        Assert.True(await repository.IsAssetInUseAsync(usedAsset.Id));
        Assert.False(await repository.IsAssetInUseAsync(asset.Id));
        Assert.Contains(await repository.GetUnusedOlderThanAsync(DateTime.UtcNow.AddDays(1), batchSize: 10), item => item.Id == asset.Id);

        await repository.DeleteAsync(asset);
        await context.SaveChangesAsync();
        Assert.Null(await repository.GetByIdAsync(asset.Id));
    }

    private static async Task CoverAdminUserRoleAuditRepositoryAsync(
        FoodDiaryDbContext context,
        UserId userId,
        UserId actorUserId) {
        Role role = await context.Roles.FirstOrDefaultAsync(item => item.Name == RoleNames.Premium) ?? Role.Create(RoleNames.Premium);
        if (context.Entry(role).State == EntityState.Detached) {
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        context.UserRoleAuditEvents.Add(UserRoleAuditEvent.Create(userId, role, UserRoleAuditAction.Removed, actorUserId, "coverage", DateTime.UtcNow));
        await context.SaveChangesAsync();

        var repository = new AdminUserRoleAuditRepository(context);
        IReadOnlyList<Application.Abstractions.Admin.Models.AdminUserRoleAuditEventReadModel> events =
            await repository.GetRecentForUserAsync(userId.Value, limit: 0);

        Application.Abstractions.Admin.Models.AdminUserRoleAuditEventReadModel auditEvent = Assert.Single(events);
        Assert.Equal(actorUserId.Value, auditEvent.ActorUserId);
        Assert.NotNull(auditEvent.ActorEmail);
    }

    private static async Task CoverBillingTransactionRunnerAsync(FoodDiaryDbContext context) {
        var runner = new EfBillingTransactionRunner(context);
        bool executed = false;

        await runner.ExecuteAsync(token => {
            executed = !token.IsCancellationRequested;
            return Task.CompletedTask;
        });

        Assert.True(executed);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            runner.ExecuteAsync(_ => throw CreateDuplicateUpdateException("IX_BillingPayments_Provider_ExternalPaymentId")));
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            runner.ExecuteAsync(_ => throw CreateDuplicateUpdateException("IX_BillingWebhookEvents_Provider_EventId")));
    }

    private static DbUpdateException CreateDuplicateUpdateException(string constraintName) =>
        new(
            "duplicate",
            new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                detail: string.Empty,
                hint: string.Empty,
                position: 0,
                internalPosition: 0,
                internalQuery: string.Empty,
                where: string.Empty,
                schemaName: string.Empty,
                tableName: string.Empty,
                columnName: string.Empty,
                dataTypeName: string.Empty,
                constraintName,
                file: string.Empty,
                line: string.Empty,
                routine: string.Empty));

    private static async Task AssertRecipeRepositoryQueriesAsync(
        FoodDiaryDbContext context,
        RecipeRepository repository,
        UserId ownerId,
        Recipe publicRecipe,
        Recipe privateRecipe,
        Recipe otherPublic) {
        var recipeOverviewReadService = new RecipeOverviewReadService(context);
        (IReadOnlyList<RecipeOverviewReadItem> searchedItems, int searchedTotal) =
            await recipeOverviewReadService.GetPagedAsync(ownerId, includePublic: false, page: 0, limit: 0, filters: new RecipeQueryFilters("100% Pancake"));
        Recipe? withSteps = await repository.GetByIdAsync(publicRecipe.Id, ownerId, includeSteps: true, asTracking: true);
        Assert.NotNull(withSteps);
        withSteps.ApplyComputedNutrition(220, 9, 5, 31, 4, 0);
        await repository.UpdateNutritionAsync(withSteps);
        await context.SaveChangesAsync();
        context.Entry(withSteps).State = EntityState.Detached;
        await repository.UpdateNutritionAsync(withSteps);
        await context.SaveChangesAsync();
        var missingRecipe = Recipe.Create(ownerId, "Missing nutrition", servings: 1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => repository.UpdateNutritionAsync(missingRecipe));

        IReadOnlyDictionary<RecipeId, Recipe> emptyByIds = await repository.GetByIdsAsync([], ownerId);
        IReadOnlyDictionary<RecipeId, Recipe> byIds = await repository.GetByIdsAsync([publicRecipe.Id, otherPublic.Id, otherPublic.Id], ownerId);
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> emptyUsage = await recipeOverviewReadService.GetByIdsWithUsageAsync([], ownerId);
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> usage = await recipeOverviewReadService.GetByIdsWithUsageAsync([publicRecipe.Id], ownerId);
        (IReadOnlyList<RecipeOverviewReadItem> exploreItems, int exploreTotal) =
            await recipeOverviewReadService.GetExplorePagedAsync(ownerId, page: 1, limit: 10, search: "Public", category: "Salad", maxPrepTime: 10, sortBy: "popular");

        await repository.DeleteAsync(privateRecipe);
        await context.SaveChangesAsync();
        await repository.DeleteAsync(privateRecipe);

        Assert.Single(searchedItems);
        Assert.Equal(1, searchedTotal);
        Assert.Single(withSteps.Steps);
        Assert.Empty(emptyByIds);
        Assert.Equal(2, byIds.Count);
        Assert.Empty(emptyUsage);
        Assert.Equal(1, usage[publicRecipe.Id].UsageCount);
        Assert.Single(exploreItems);
        Assert.Equal(1, exploreTotal);
        Assert.Null(await repository.GetByIdAsync(privateRecipe.Id, ownerId, includePublic: false));
    }

    private static async Task CoverUserLoginEventRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new UserLoginEventRepository(context);
        DateTime now = DateTime.UtcNow;
        await repository.AddAsync(UserLoginEvent.Create(
            userId,
            "password",
            "127.0.0.1",
            "agent",
            "Chrome",
            "1",
            "Windows",
            "Desktop",
            now.AddDays(-2)));
        await repository.AddAsync(UserLoginEvent.Create(
            userId,
            "telegram",
            ipAddress: null,
            userAgent: null,
            browserName: null,
            browserVersion: null,
            operatingSystem: null,
            deviceType: null,
            now));
        await context.SaveChangesAsync();

        (IReadOnlyList<Application.Abstractions.Authentication.Models.UserLoginEventReadModel> items, int total) =
            await repository.GetPagedAsync(page: 0, limit: 500, userId.Value, search: "Chrome");
        IReadOnlyList<Application.Abstractions.Authentication.Models.UserLoginDeviceSummaryModel> summary =
            await repository.GetDeviceSummaryAsync(now.AddDays(-3), now.AddDays(1));
        int deletedNone = await repository.DeleteOlderThanAsync(now.AddDays(-10), batchSize: 0);
        int deleted = await repository.DeleteOlderThanAsync(now.AddDays(-1), batchSize: 0);

        Assert.Single(items);
        Assert.Equal(1, total);
        Assert.NotEmpty(summary);
        Assert.Equal(0, deletedNone);
        Assert.Equal(1, deleted);
    }

    private static async Task CoverRefreshTokenSessionRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new RefreshTokenSessionRepository(context);
        DateTime now = DateTime.UtcNow;
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            userId,
            "refresh-hash",
            rememberMe: true,
            authProvider: "password",
            ipAddress: "127.0.0.1",
            userAgent: "agent",
            now);
        await repository.AddAsync(session);
        await context.SaveChangesAsync();
        session.Rotate("refresh-hash-2", rememberMe: false, now.AddMinutes(1), TimeSpan.FromMinutes(5));
        await repository.UpdateAsync(session);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByIdAsync(session.Id));
        Assert.Single(await repository.GetActiveByUserIdAsync(userId));

        session.Revoke(now.AddMinutes(2));
        await repository.UpdateAsync(session);
        await context.SaveChangesAsync();

        Assert.Empty(await repository.GetActiveByUserIdAsync(userId));
    }

    private static void AddCycleDetails(CycleProfile profile, DateTime today) {
        profile.UpsertBleedingEntry(today.AddDays(-3), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 4, notes: "start");
        profile.UpsertBleedingEntry(today.AddDays(-2), BleedingType.Spotting, CycleFlowLevel.Light, painImpact: 1, notes: "spotting");
        profile.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 3, ["lower"], "minor");
        profile.UpsertFactor(CycleFactorType.NonHormonalContraception, today.AddDays(-5), today.AddDays(-1), "tracking");
        profile.UpsertFertilitySignal(today, 36.7, OvulationTestResult.Negative, "sticky", hadSex: false, notes: "signal");
    }

    private static async Task CoverNotificationRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new NotificationRepository(context, FixedTime);
        Notification transient = await repository.AddAsync(Notification.Create(userId, "transient", "{}", "one"));
        Notification standard = await repository.AddAsync(Notification.Create(userId, "standard", "{}", "two"));
        await repository.AddAsync(Notification.Create(userId, "standard", "{}", "three"));
        await context.SaveChangesAsync();

        Notification? tracked = await repository.GetByIdAsync(standard.Id, asTracking: true);
        Assert.NotNull(tracked);
        tracked.MarkAsRead();
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByIdAsync(standard.Id));
        Assert.True(await repository.ExistsAsync(userId, "standard", "two"));
        Assert.Equal(3, (await repository.GetByUserAsync(userId, limit: 10)).Count);
        Assert.Equal(2, await repository.GetUnreadCountAsync(userId));
        Assert.Equal(1, await repository.GetUnreadCountAsync(userId, "standard"));

        int emptyDeleted = await repository.DeleteExpiredBatchAsync(
            ["transient"],
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(-1),
            DateTime.UtcNow.AddYears(-1),
            batchSize: 0);
        transient.MarkAsRead();
        await repository.UpdateAsync(transient);
        await context.SaveChangesAsync();
        int deleted = await repository.DeleteExpiredBatchAsync(
            ["transient"],
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1),
            batchSize: 10);
        await context.SaveChangesAsync();

        await repository.MarkAllReadAsync(userId);

        Assert.Equal(0, emptyDeleted);
        Assert.Equal(3, deleted);
        Assert.Equal(0, await repository.GetUnreadCountAsync(userId));
    }

    private static async Task CoverWebPushSubscriptionRepositoryAsync(FoodDiaryDbContext context, UserId userId) {
        var repository = new WebPushSubscriptionRepository(context);
        await repository.DeleteRangeAsync([]);

        WebPushSubscription subscription = await repository.AddAsync(WebPushSubscription.Create(
            userId,
            $"https://push.example.com/{Guid.NewGuid():N}",
            "p256",
            "auth",
            DateTime.UtcNow.AddDays(1),
            locale: "en",
            userAgent: "tests"));
        await context.SaveChangesAsync();
        WebPushSubscription? tracked = await repository.GetByEndpointAsync(subscription.Endpoint, asTracking: true);
        Assert.NotNull(tracked);
        tracked.Refresh(userId, "p256-new", "auth-new", DateTime.UtcNow.AddDays(2), locale: "ru", userAgent: "tests/2");
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByEndpointAsync(subscription.Endpoint));
        Assert.Single(await repository.GetByUserAsync(userId));
        await repository.DeleteRangeAsync([tracked]);
        await context.SaveChangesAsync();

        WebPushSubscription second = await repository.AddAsync(WebPushSubscription.Create(
            userId,
            $"https://push.example.com/{Guid.NewGuid():N}",
            "p256",
            "auth"));
        await context.SaveChangesAsync();
        await repository.DeleteAsync(second);
        await context.SaveChangesAsync();

        Assert.Empty(await repository.GetByUserAsync(userId));
    }

    private static async Task CoverBillingRepositoriesAsync(FoodDiaryDbContext context, UserId userId) {
        DateTime now = DateTime.UtcNow;
        var subscriptionRepository = new BillingSubscriptionRepository(context);
        var subscription = BillingSubscription.CreatePending(userId, BillingProviderNames.Stripe, "cus_test", "price_test", "premium");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            externalSubscriptionId: "sub_test",
            externalPaymentMethodId: "pm_test",
            externalPriceId: "price_test",
            plan: "premium",
            status: "active",
            currentPeriodStartUtc: now.AddDays(-30),
            currentPeriodEndUtc: now.AddDays(-1),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            webhookEventId: "evt_snapshot",
            syncedAtUtc: now);
        await subscriptionRepository.AddAsync(subscription);
        await context.SaveChangesAsync();

        Assert.NotNull(await subscriptionRepository.GetByUserIdAsync(userId));
        Assert.NotNull(await subscriptionRepository.GetByExternalCustomerIdAsync(BillingProviderNames.Stripe, "cus_test"));
        Assert.NotNull(await subscriptionRepository.GetByExternalSubscriptionIdAsync(BillingProviderNames.Stripe, "sub_test"));
        Assert.NotNull(await subscriptionRepository.GetByExternalPaymentMethodIdAsync(BillingProviderNames.Stripe, "pm_test"));
        Assert.Single(await subscriptionRepository.GetDueForRenewalAsync(BillingProviderNames.Stripe, now, limit: 10));

        subscription.MarkPremiumRoleManagedByBilling(value: true, changedAtUtc: now);
        await subscriptionRepository.UpdateAsync(subscription);
        await context.SaveChangesAsync();

        await CoverBillingPaymentRepositoryAsync(context, userId, subscription.Id, now);
        await CoverBillingWebhookEventRepositoryAsync(context, now);
    }

    private static async Task CoverBillingPaymentRepositoryAsync(
        FoodDiaryDbContext context,
        UserId userId,
        Guid subscriptionId,
        DateTime now) {
        var paymentRepository = new BillingPaymentRepository(context);
        BillingPayment payment = CreateBillingPayment(userId, subscriptionId, "pay_test", "evt_pay", now);
        await paymentRepository.AddAsync(payment);
        await context.SaveChangesAsync();
        Assert.NotNull(await paymentRepository.GetByExternalPaymentIdAsync(BillingProviderNames.Stripe, "pay_test"));
        var billingTransactionRunner = new EfBillingTransactionRunner(context);

        await Assert.ThrowsAsync<BillingPaymentAlreadyExistsException>(
            () => billingTransactionRunner.ExecuteAsync(
                ct => paymentRepository.AddAsync(
                    CreateBillingPayment(userId, subscriptionId, "pay_test", "evt_pay_duplicate", now),
                    ct)));
    }

    private static async Task CoverBillingWebhookEventRepositoryAsync(FoodDiaryDbContext context, DateTime now) {
        var webhookRepository = new BillingWebhookEventRepository(context);
        var webhookEvent = BillingWebhookEvent.CreateProcessed(
            BillingProviderNames.Stripe,
            "evt_test",
            "invoice.paid",
            "invoice_test",
            now,
            "{}");
        await webhookRepository.AddAsync(webhookEvent);
        await context.SaveChangesAsync();
        Assert.True(await webhookRepository.ExistsAsync(BillingProviderNames.Stripe, "evt_test"));
        var billingTransactionRunner = new EfBillingTransactionRunner(context);

        await Assert.ThrowsAsync<BillingWebhookEventAlreadyProcessedException>(
            () => billingTransactionRunner.ExecuteAsync(
                ct => webhookRepository.AddAsync(
                    BillingWebhookEvent.CreateProcessed(
                        BillingProviderNames.Stripe,
                        "evt_test",
                        "invoice.paid",
                        "invoice_test_2",
                        now,
                        "{}"),
                    ct)));
    }

    private static async Task CoverRecommendationRepositoryAsync(
        FoodDiaryDbContext context,
        UserId dietologistUserId,
        UserId clientUserId) {
        var repository = new RecommendationRepository(context);
        Recommendation recommendation = await repository.AddAsync(Recommendation.Create(dietologistUserId, clientUserId, "More protein"));
        await context.SaveChangesAsync();
        Recommendation? tracked = await repository.GetByIdAsync(recommendation.Id, asTracking: true);
        Assert.NotNull(tracked);
        tracked.MarkAsRead();
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByIdAsync(recommendation.Id));
        Assert.Single(await repository.GetByClientAsync(clientUserId, limit: 5));
        Assert.Single(await repository.GetByDietologistAndClientAsync(dietologistUserId, clientUserId, limit: 5));
        Assert.Equal(0, await repository.GetUnreadCountAsync(clientUserId));
    }

    private static async Task CoverDietologistInvitationRepositoryAsync(
        FoodDiaryDbContext context,
        UserId dietologistUserId,
        UserId clientUserId) {
        var repository = new DietologistInvitationRepository(context);
        var invitation = DietologistInvitation.Create(
            clientUserId,
            "dietologist@example.com",
            "token_hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled);
        invitation.Accept(dietologistUserId);
        await repository.AddAsync(invitation);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        DietologistInvitation? detached = await repository.GetByIdAsync(invitation.Id);
        Assert.NotNull(detached);
        detached.UpdatePermissions(new DietologistPermissions(ShareMeals: false));
        await repository.UpdateAsync(detached);
        await context.SaveChangesAsync();

        var missingInvitation = DietologistInvitation.Create(
            clientUserId,
            "missing-dietologist@example.com",
            "missing_token_hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => repository.UpdateAsync(missingInvitation));

        Assert.NotNull(await repository.GetByIdAsync(invitation.Id, asTracking: true));
        Assert.NotNull(await repository.GetActiveByClientAsync(clientUserId, asTracking: true));
        Assert.NotNull(await repository.GetActiveByClientAndDietologistAsync(clientUserId, dietologistUserId));
        Assert.True(await repository.HasActiveRelationshipAsync(clientUserId, dietologistUserId));
        Assert.Single(await repository.GetActiveByDietologistAsync(dietologistUserId));
        Assert.NotNull(await repository.GetByClientAndStatusAsync(clientUserId, DietologistInvitationStatus.Accepted));

        var pendingClient = User.Create($"pending-client-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(pendingClient);
        await context.SaveChangesAsync();
        await repository.AddAsync(DietologistInvitation.Create(
            pendingClient.Id,
            "pending-dietologist@example.com",
            "pending_token_hash",
            DateTime.UtcNow.AddDays(7),
            DietologistPermissions.AllEnabled));
        await context.SaveChangesAsync();

        DietologistInvitationReadModel? pendingReadModel = await repository.GetByClientAndStatusReadModelAsync(
            pendingClient.Id,
            DietologistInvitationStatus.Pending);
        Assert.NotNull(pendingReadModel);
        Assert.Null(pendingReadModel.DietologistUserId);
    }

    private static BillingPayment CreateBillingPayment(
        UserId userId,
        Guid subscriptionId,
        string externalPaymentId,
        string webhookEventId,
        DateTime now) =>
        BillingPayment.Create(
            userId,
            subscriptionId,
            BillingProviderNames.Stripe,
            externalPaymentId,
            "cus_test",
            "sub_test",
            "pm_test",
            "price_test",
            "premium",
            "paid",
            "renewal",
            amount: 10,
            currency: "USD",
            currentPeriodStartUtc: now.AddDays(-30),
            currentPeriodEndUtc: now.AddDays(30),
            webhookEventId,
            providerMetadataJson: "{}");

    private static async Task CoverFavoriteProductRepositoryAsync(
        FoodDiaryDbContext context,
        UserId userId,
        ProductId productId) {
        var repository = new FavoriteProductRepository(context);
        Assert.Empty(await repository.GetByProductIdsAsync(userId, []));
        FavoriteProduct favorite = await repository.AddAsync(FavoriteProduct.Create(userId, productId, "Rice", 120));
        await context.SaveChangesAsync();
        Assert.NotNull(await repository.GetByIdAsync(favorite.Id, userId));
        FavoriteProduct? tracked = await repository.GetByIdAsync(favorite.Id, userId, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateName("Brown rice");
        tracked.UpdatePreferredPortionAmount(150);
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByProductIdAsync(productId, userId));
        Assert.Single(await repository.GetAllAsync(userId));
        Assert.True((await repository.GetByProductIdsAsync(userId, [productId])).ContainsKey(productId));

        await repository.DeleteAsync(tracked);
        await context.SaveChangesAsync();

        Assert.Null(await repository.GetByProductIdAsync(productId, userId));
    }

    private static async Task CoverFavoriteRecipeRepositoryAsync(
        FoodDiaryDbContext context,
        UserId userId,
        RecipeId recipeId) {
        var repository = new FavoriteRecipeRepository(context);
        Assert.Empty(await repository.GetByRecipeIdsAsync(userId, []));
        FavoriteRecipe favorite = await repository.AddAsync(FavoriteRecipe.Create(userId, recipeId, "Dinner recipe"));
        await context.SaveChangesAsync();
        FavoriteRecipe? tracked = await repository.GetByIdAsync(favorite.Id, userId, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateName("Weekend recipe");
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByRecipeIdAsync(recipeId, userId));
        Assert.Single(await repository.GetAllAsync(userId));
        Assert.True((await repository.GetByRecipeIdsAsync(userId, [recipeId])).ContainsKey(recipeId));

        await repository.DeleteAsync(tracked);
        await context.SaveChangesAsync();

        Assert.Null(await repository.GetByRecipeIdAsync(recipeId, userId));
    }

    private static async Task CoverFavoriteMealRepositoryAsync(
        FoodDiaryDbContext context,
        UserId userId,
        MealId mealId) {
        var repository = new FavoriteMealRepository(context);
        Assert.Empty(await repository.GetByMealIdsAsync(userId, []));
        FavoriteMeal favorite = await repository.AddAsync(FavoriteMeal.Create(userId, mealId, "Dinner meal"));
        await context.SaveChangesAsync();
        FavoriteMeal? untracked = await repository.GetByIdAsync(favorite.Id, userId);
        Assert.NotNull(untracked);
        FavoriteMeal? tracked = await repository.GetByIdAsync(favorite.Id, userId, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateName("Weekend dinner");
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetByMealIdAsync(mealId, userId));
        Assert.Single(await repository.GetAllAsync(userId));
        Assert.True((await repository.GetByMealIdsAsync(userId, [mealId])).ContainsKey(mealId));

        await repository.DeleteAsync(tracked);
        await context.SaveChangesAsync();

        Assert.Null(await repository.GetByMealIdAsync(mealId, userId));
    }

    private static async Task CoverFastingOccurrenceAndCheckInsAsync(
        FoodDiaryDbContext context,
        UserId userId,
        FastingPlanId planId,
        DateTime now) {
        var occurrenceRepository = new FastingOccurrenceRepository(context);
        var active = FastingOccurrence.Create(planId, userId, FastingOccurrenceKind.FastingWindow, now.AddHours(-4), 1, targetHours: 16);
        var scheduled = FastingOccurrence.Schedule(planId, userId, FastingOccurrenceKind.EatingWindow, now.AddHours(20), 2, targetHours: 8);
        await occurrenceRepository.AddAsync(active);
        await occurrenceRepository.AddAsync(scheduled);
        await context.SaveChangesAsync();
        active.UpdateCheckIn(2, 4, 5, ["hungry"], "Fine", now.AddHours(-1));
        await occurrenceRepository.UpdateAsync(active);
        await context.SaveChangesAsync();

        Assert.NotEmpty(await occurrenceRepository.GetActiveAsync());
        Assert.NotNull(await occurrenceRepository.GetCurrentAsync(userId, asTracking: true));
        Assert.NotNull(await occurrenceRepository.GetByIdAsync(active.Id, asTracking: true));
        Assert.Equal(2, (await occurrenceRepository.GetByPlanAsync(planId, includeCompleted: false)).Count);
        Assert.Single(await occurrenceRepository.GetByUserAsync(userId, now.AddDays(-1), now, FastingOccurrenceStatus.Active));
        Assert.Equal(
            1,
            (await occurrenceRepository.GetPagedByUserAsync(userId, page: 0, limit: 0, status: FastingOccurrenceStatus.Active)).TotalItems);

        var checkInRepository = new FastingCheckInRepository(context);
        Assert.Empty(await checkInRepository.GetByOccurrenceIdsAsync([]));
        await checkInRepository.AddAsync(FastingCheckIn.Create(active.Id, userId, 2, 4, 5, ["hungry"], "Ok", now));
        await context.SaveChangesAsync();
        Assert.Single(await checkInRepository.GetByOccurrenceIdsAsync([active.Id]));
    }

    private static async Task<FastingSession> CoverFastingSessionsAsync(
        FoodDiaryDbContext context,
        UserId userId,
        DateTime now) {
        var repository = new FastingSessionRepository(context, FixedTime);
        Assert.Equal(0, await repository.GetCurrentStreakAsync(UserId.New()));
        FastingSession currentSession = await repository.AddAsync(FastingSession.Create(userId, FastingProtocol.F16_8, 16, now.AddHours(-2)));
        FastingSession completedSession = await repository.AddAsync(FastingSession.Create(userId, FastingProtocol.F24_0, 24, now.AddDays(-2)));
        completedSession.End(now.AddDays(-1));
        await repository.UpdateAsync(completedSession);
        await context.SaveChangesAsync();
        context.Entry(completedSession).State = EntityState.Detached;
        completedSession.UpdateNotes("Detached update");
        await repository.UpdateAsync(completedSession);
        Assert.Equal(EntityState.Modified, context.Entry(completedSession).State);
        context.Entry(completedSession).State = EntityState.Detached;
        var yesterdaySession = FastingSession.Create(userId, FastingProtocol.F16_8, 16, FixedNow.Date.AddDays(-1).AddHours(1));
        yesterdaySession.End(FixedNow.Date.AddHours(1));
        await repository.AddAsync(yesterdaySession);
        await context.SaveChangesAsync();
        var oldStreakUser = User.Create($"old-streak-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(oldStreakUser);
        await context.SaveChangesAsync();
        var oldSession = FastingSession.Create(oldStreakUser.Id, FastingProtocol.F16_8, 16, FixedNow.Date.AddDays(-5).AddHours(1));
        oldSession.End(FixedNow.Date.AddDays(-4).AddHours(1));
        await repository.AddAsync(oldSession);
        await context.SaveChangesAsync();

        Assert.NotNull(await repository.GetCurrentAsync(userId));
        Assert.NotNull(await repository.GetByIdAsync(currentSession.Id));
        Assert.NotNull(await repository.GetByIdAsync(currentSession.Id, asTracking: true));
        Assert.Equal(3, (await repository.GetHistoryAsync(userId, now.AddDays(-3), now)).Count);
        Assert.Equal(2, await repository.GetCompletedCountAsync(userId));
        Assert.True(await repository.GetCurrentStreakAsync(userId) > 0);
        Assert.Equal(0, await repository.GetCurrentStreakAsync(oldStreakUser.Id));

        return currentSession;
    }

    private static async Task CoverFastingTelemetryAsync(
        FoodDiaryDbContext context,
        FastingSessionId sessionId,
        DateTime now) {
        var repository = new FastingTelemetryEventRepository(context);
        await repository.AddAsync(new FastingTelemetryEventRecord(
            Name: "fasting.started",
            OccurredAtUtc: now,
            SessionId: sessionId.Value.ToString(),
            Protocol: FastingProtocol.F16_8.ToString(),
            PlanType: FastingPlanType.Intermittent.ToString(),
            Status: FastingOccurrenceStatus.Active.ToString(),
            OccurrenceKind: FastingOccurrenceKind.FastingWindow.ToString(),
            ReminderPresetId: "preset",
            ReminderSource: "manual",
            FirstReminderHours: 1,
            FollowUpReminderHours: 2,
            PlannedDurationHours: 16,
            ActualDurationHours: null,
            HungerLevel: 2,
            EnergyLevel: 4,
            MoodLevel: 5,
            SymptomsCount: 1,
            HadNotes: true));
        await context.SaveChangesAsync();

        Assert.Single(await repository.GetSinceAsync(now.AddMinutes(-1)));
    }
    private static Product CreateProduct(UserId userId, string name) =>
        Product.Create(
            userId,
            name,
            MeasurementUnit.G,
            100,
            100,
            caloriesPerBase: 120,
            proteinsPerBase: 3,
            fatsPerBase: 1,
            carbsPerBase: 20,
            fiberPerBase: 2,
            alcoholPerBase: 0,
            imageUrl: null);

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
