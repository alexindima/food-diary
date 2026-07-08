using System.Reflection;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
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
        var shoppingListId = ShoppingListId.New();
        var shoppingListItemId = ShoppingListItemId.New();
        var productId = ProductId.New();
        var occurredOnUtc = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
        DateTime deletedAtUtc = occurredOnUtc.AddMinutes(-1);
        DateTime checkedOnUtc = occurredOnUtc.AddMinutes(-2);

        var autoNutrition = new RecipeAutoNutritionEnabledDomainEvent(recipeId, occurredOnUtc);
        var manualNutrition = new RecipeManualNutritionSetDomainEvent(recipeId, occurredOnUtc);
        var userDeleted = new UserDeletedDomainEvent(userId, deletedAtUtc, occurredOnUtc);
        var userRestored = new UserRestoredDomainEvent(userId, occurredOnUtc);
        var itemAdded = new ShoppingListItemAddedDomainEvent(
            shoppingListId,
            shoppingListItemId,
            productId,
            "Apple",
            2,
            MeasurementUnit.Pcs,
            "Fruit",
            "A1",
            "Ripe",
            isChecked: true,
            checkedOnUtc,
            sortOrder: 3,
            occurredOnUtc);
        var itemsCleared = new ShoppingListItemsClearedDomainEvent(shoppingListId, 2, occurredOnUtc);
        var nameUpdated = new ShoppingListNameUpdatedDomainEvent(shoppingListId, "Old", "New", occurredOnUtc);

        Assert.Multiple(
            () => Assert.Equal(recipeId, autoNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, autoNutrition.OccurredOnUtc),
            () => Assert.Equal(recipeId, manualNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, manualNutrition.OccurredOnUtc),
            () => Assert.Equal(userId, userDeleted.UserId),
            () => Assert.Equal(deletedAtUtc, userDeleted.DeletedAtUtc),
            () => Assert.Equal(occurredOnUtc, userDeleted.OccurredOnUtc),
            () => Assert.Equal(userId, userRestored.UserId),
            () => Assert.Equal(occurredOnUtc, userRestored.OccurredOnUtc),
            () => Assert.Equal(productId, itemAdded.ProductId),
            () => Assert.Equal("Apple", itemAdded.Name),
            () => Assert.Equal(2, itemAdded.Amount),
            () => Assert.Equal(MeasurementUnit.Pcs, itemAdded.Unit),
            () => Assert.Equal("Fruit", itemAdded.Category),
            () => Assert.Equal("A1", itemAdded.Aisle),
            () => Assert.Equal("Ripe", itemAdded.Note),
            () => Assert.True(itemAdded.IsChecked),
            () => Assert.Equal(checkedOnUtc, itemAdded.CheckedOnUtc),
            () => Assert.Equal(3, itemAdded.SortOrder),
            () => Assert.Equal(shoppingListId, itemsCleared.ShoppingListId),
            () => Assert.Equal(2, itemsCleared.ClearedItemsCount),
            () => Assert.Equal(occurredOnUtc, itemsCleared.OccurredOnUtc),
            () => Assert.Equal(shoppingListId, nameUpdated.ShoppingListId),
            () => Assert.Equal("Old", nameUpdated.PreviousName),
            () => Assert.Equal("New", nameUpdated.CurrentName),
            () => Assert.Equal(occurredOnUtc, nameUpdated.OccurredOnUtc));
    }

    [Fact]
    public void UserSecurityAndAdminMethods_CoverNoOpAndUpdatePaths() {
        var user = User.Create("user@example.com", "hash");
        DateTime occurredAtUtc = DateTime.UtcNow;

        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithAuthenticationActivity(occurredAtUtc);
        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(IsEmailConfirmed: null));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: null));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "en"));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "en"));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate());
        user.RecordAuthenticationActivity(occurredAtUtc);
        user.UpdatePassword("next-hash");
        user.SetEmailConfirmationToken("email-token", occurredAtUtc.AddHours(1), occurredAtUtc);

        Assert.Multiple(
            () => Assert.Equal(occurredAtUtc, state.LastLoginAtUtc),
            () => Assert.Equal("next-hash", user.Password),
            () => Assert.Equal("email-token", user.EmailConfirmationTokenHash),
            () => Assert.Equal("en", user.Language));
    }

    [Fact]
    public void EntityNavigationAndPrivateConstructors_AreCoveredForEfOnlyMembers() {
        Product product = CreateProduct();
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
            FastingProtocol.F16_8,
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
            CreatePrivate<WeightEntry>(),
            CreatePrivate<WaistEntry>(),
            CreatePrivate<ImageAsset>(),
            CreatePrivate<UserRole>(),
            CreatePrivate<CycleProfile>(),
            product,
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
            () => Assert.Equal(0, product.UsageCount),
            () => Assert.Null(product.UsdaFood),
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
        Product product = CreateProduct();
        Recipe recipe = CreateRecipe();

        source.ApplyProductSnapshot(product);
        source.ApplySource(MealAiItemId.New(), MealItemOrigin.AIText);
        source.ApplySource(source.SourceAiItemId, MealItemOrigin.AIText);
        target.CopySourceAndSnapshotFrom(source);
        target.ApplyRecipeSnapshot(recipe);
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
    public void ShoppingItems_CoverSourceAndUpdatePaths() {
        var item = ShoppingListItem.Create(
            ShoppingListId.New(),
            " Apple ",
            ProductId.New(),
            1,
            MeasurementUnit.Pcs,
            " Fruit ",
            isChecked: true,
            sortOrder: 1,
            aisle: " A1 ",
            note: " Ripe ",
            checkedOnUtc: DateTime.UtcNow);

        item.UpdateDetails(
            " Pear ",
            ProductId.New(),
            2,
            MeasurementUnit.Pcs,
            " Fruit ",
            " A2 ",
            " Green ",
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 2);
        ShoppingListItemSource source = item.AddMealPlanSource(
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            " Dinner ",
            dayNumber: 1,
            " Lunch ",
            amount: 2,
            MeasurementUnit.Pcs);

        ReadPublicProperties(item);
        ReadPublicProperties(source);

        Assert.Multiple(
            () => Assert.Equal("Pear", item.Name),
            () => Assert.Null(item.CheckedOnUtc),
            () => Assert.Single(item.Sources),
            () => Assert.Equal("Dinner", source.Label),
            () => Assert.Equal("Lunch", source.MealType));
    }

    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), "Apple", 100);
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
            DateTime.UtcNow,
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
                DateTime.UtcNow.Date.AddDays(-day),
                BleedingType.Bleeding,
                CycleFlowLevel.Medium,
                painImpact: null,
                notes: null);
        }
        profile.UpsertSymptomEntry(
            DateTime.UtcNow.Date,
            CycleSymptomCategory.Mood,
            intensity: 5,
            tags: ["calm"],
            note: null);
        profile.UpsertFertilitySignal(
            DateTime.UtcNow.Date,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);
        bool cleared = profile.ClearDay(DateTime.UtcNow.Date);
        ReadPublicProperties(profile);

        Assert.Multiple(
            () => Assert.True(cleared),
            () => Assert.Equal(CycleConfidence.Medium, profile.Confidence),
            () => Assert.Equal("updated", profile.Notes));
    }

    private static Product CreateProduct() =>
        Product.Create(
            UserId.New(),
            "Apple",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

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
