using System.Reflection;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DomainCoverageCompletionTests {

    [Fact]
    public void EventProperties_ExposeConstructorValues() {
        var recipeId = RecipeId.New();
        var userId = UserId.New();
        var occurredOnUtc = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
        DateTime deletedAtUtc = occurredOnUtc.AddMinutes(-1);

        var autoNutrition = new RecipeAutoNutritionEnabledDomainEvent(recipeId, occurredOnUtc);
        var manualNutrition = new RecipeManualNutritionSetDomainEvent(recipeId, occurredOnUtc);
        var userDeleted = new UserDeletedDomainEvent(userId, deletedAtUtc, occurredOnUtc);
        var userRestored = new UserRestoredDomainEvent(userId, occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(recipeId, autoNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, autoNutrition.OccurredOnUtc),
            () => Assert.Equal(recipeId, manualNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, manualNutrition.OccurredOnUtc),
            () => Assert.Equal(userId, userDeleted.UserId),
            () => Assert.Equal(deletedAtUtc, userDeleted.DeletedAtUtc),
            () => Assert.Equal(occurredOnUtc, userDeleted.OccurredOnUtc),
            () => Assert.Equal(userId, userRestored.UserId),
            () => Assert.Equal(occurredOnUtc, userRestored.OccurredOnUtc));
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        Recipe recipe = CreateRecipe();
        var mealPlan = MealPlan.CreateForUser(
            UserId.New(),
            name: "Plan",
            description: null,
            DietType.Balanced,
            durationDays: 1,
            targetCaloriesPerDay: null);
        var invitation = DietologistInvitation.Create(
            UserId.New(),
            dietologistEmail: "dietologist@example.com",
            tokenHash: "token",
            expiresAtUtc: DateTime.UtcNow.AddDays(1),
            new DietologistPermissions(
                ShareMeals: true,
                ShareStatistics: true,
                ShareWeight: true,
                ShareWaist: true,
                ShareGoals: true,
                ShareHydration: true,
                ShareProfile: true,
                ShareFasting: true));
        var fastingSession = FastingSession.Create(
            UserId.New(),
            FastingProtocol.Fast16Eat8,
            plannedDurationHours: 16,
            startedAtUtc: DateTime.UtcNow);
        var webhookEvent = BillingWebhookEvent.CreateProcessed(
            BillingProviderNames.YooKassa,
            eventId: "evt",
            eventType: "payment.succeeded",
            externalObjectId: "payment",
            processedAtUtc: DateTime.UtcNow,
            payloadJson: "{}");
        var usdaFood = new UsdaFood {
            FdcId = 1,
            Description = "Apple",
            FoodCategoryId = 10,
            FoodCategory = "Fruit",
        };

        object[] instances = [
            CreatePrivate<ImageAsset>(),
            CreatePrivate<UserRole>(),
            CreatePrivate<CycleProfile>(),
            recipe,
            mealPlan,
            invitation,
            fastingSession,
            webhookEvent,
            usdaFood,
        ];

        foreach (object instance in instances) {
            ReadPublicProperties(instance);
        }

        Assert.Multiple(
            () => Assert.Null(mealPlan.User),
            () => Assert.Null(invitation.DietologistUser),
            () => Assert.Null(fastingSession.User),
            () => Assert.Equal(BillingProviderNames.YooKassa, webhookEvent.Provider),
            () => Assert.Equal(10, usdaFood.FoodCategoryId),
            () => Assert.Equal("Fruit", usdaFood.FoodCategory));
    }

    [Fact]
    public void MealItem_SourceSnapshotAndRecipeSnapshot_CoverRemainingPaths() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow);
        MealItem source = meal.AddProduct(ProductId.New(), 100);
        MealItem target = meal.AddRecipe(RecipeId.New(), 2);
        MealAiSession aiSession = meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create(
                    nameEn: "Apple",
                    nameLocal: null,
                    amount: 100,
                    unit: "g",
                    calories: 52,
                    proteins: 0.3,
                    fats: 0.2,
                    carbs: 14,
                    fiber: 2.4,
                    alcohol: 0),
            ]);
        MealAiItem aiItem = Assert.Single(aiSession.Items);
        RecipeStep step = CreateRecipe().AddStep(1, "Step");
        RecipeIngredient ingredient = step.AddProductIngredient(ProductId.New(), 100);
        Recipe recipe = CreateRecipe();

        source.ApplyProductSnapshot("Apple", imageUrl: null, MeasurementUnit.G, baseAmount: 100,
            caloriesPerBase: 52, proteinsPerBase: 0.3, fatsPerBase: 0.2, carbsPerBase: 14,
            fiberPerBase: 2.4, alcoholPerBase: 0);
        source.ApplySource(MealAiItemId.New(), MealItemOrigin.AiText);
        source.ApplySource(source.SourceAiItemId, MealItemOrigin.AiText);
        target.CopySourceAndSnapshotFrom(source);
        target.ApplyRecipeSnapshot(recipe.Name, recipe.ImageUrl, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol);
        target.ApplySource(sourceAiItemId: null, MealItemOrigin.Barcode);
        ReadPublicProperties(source);
        ReadPublicProperties(target);
        ReadPublicProperties(aiSession);
        ReadPublicProperties(aiItem);
        ReadPublicProperties(ingredient);

        Assert.Multiple(
            () => Assert.True(source.HasNutritionSnapshot),
            () => Assert.True(target.HasNutritionSnapshot),
            () => Assert.Equal("serving", target.SnapshotUnit),
            () => Assert.Equal(MealItemOrigin.Barcode, target.Origin),
            () => Assert.Null(target.SourceAiItemId));
    }

    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), "Apple", 100);
        favorite.UpdatePreferredPortionAmount(125);
        favorite.UpdatePreferredPortionAmount(125);
        var asset = ImageAsset.Create(UserId.New(), " object/key ", " https://img ");
        var userRole = new UserRole(UserId.New(), RoleId.New());
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            " refresh ",
            rememberMe: true,
            authProvider: " local ",
            ipAddress: " 127.0.0.1 ",
            userAgent: new string('a', 600),
            DateTime.UtcNow);
        session.Rotate(" next-refresh ", rememberMe: false, DateTime.UtcNow.AddMinutes(1), TimeSpan.Zero);
        var sessionWithNullOptionals = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "refresh",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow);

        Assert.Multiple(
            () => Assert.Equal(125, favorite.PreferredPortionAmount),
            () => Assert.Equal("object/key", asset.ObjectKey),
            () => Assert.Equal("https://img", asset.Url),
            () => Assert.NotEqual(UserId.Empty, userRole.UserId),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Null(sessionWithNullOptionals.AuthProvider));
    }

    [Fact]
    public void CycleProfile_ConfidenceAndClearDay_CoverRemainingPaths() {
        var profile = CycleProfile.Create(
            UserId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            mode: CycleTrackingMode.TryingToConceive,
            averageCycleLength: null,
            averagePeriodLength: null,
            lutealLength: null,
            isRegular: true,
            notes: " notes ");
        profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.PeriodTracking,
            AverageCycleLength: null,
            AveragePeriodLength: null,
            LutealLength: null,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false,
            Notes: " updated ",
            ClearNotes: false));
        for (int day = 0; day < 9; day++) {
            profile.UpsertBleedingEntry(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-day),
                BleedingType.Bleeding,
                CycleFlowLevel.Medium,
                painImpact: null,
                notes: null);
        }
        profile.UpsertSymptomEntry(
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Mood,
            intensity: 5,
            tags: ["calm"],
            note: null);
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertFertilitySignal(
            DateOnly.FromDateTime(DateTime.UtcNow),
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);
        bool cleared = profile.ClearDay(DateOnly.FromDateTime(DateTime.UtcNow));
        ReadPublicProperties(profile);

        Assert.Multiple(
            () => Assert.True(cleared),
            () => Assert.Equal(CycleConfidence.Medium, profile.Confidence),
            () => Assert.Equal("updated", profile.Notes));
    }

    private static Recipe CreateRecipe() {
        var recipe = Recipe.Create(
            UserId.New(),
            "Recipe",
            servings: 2,
            imageUrl: "https://img");
        SetPrivateProperty(recipe, nameof(Recipe.TotalCalories), 200d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalProteins), 20d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalFats), 10d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalCarbs), 30d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalFiber), 4d);
        SetPrivateProperty(recipe, nameof(Recipe.TotalAlcohol), 2d);
        return recipe;
    }

    private static T CreatePrivate<T>() where T : class =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }

    private static void SetPrivateProperty<TValue>(object instance, string propertyName, TValue value) {
        instance.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }
}
