using System.Reflection;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ThirdPassDomainHardeningTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GoalReplacement_WhenNewGoalIsInvalid_IsAtomic() {
        var user = User.Create("goals@example.com", "hash");
        WeightGoal weightGoal = user.StartWeightGoal(70, 80, Now);
        WaistGoal waistGoal = user.StartWaistGoal(80, 90, Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWeightGoal(double.NaN, 79, Now.AddDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWaistGoal(double.PositiveInfinity, 89, Now.AddDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartWeightGoal(69, 79, DateTime.SpecifyKind(Now.AddDays(1), DateTimeKind.Unspecified)));

        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Active, weightGoal.Status),
            () => Assert.Null(weightGoal.EndedAtUtc),
            () => Assert.Single(user.WeightGoals),
            () => Assert.Equal(70, user.DesiredWeightKg),
            () => Assert.Equal(WaistGoalStatus.Active, waistGoal.Status),
            () => Assert.Null(waistGoal.EndedAtUtc),
            () => Assert.Single(user.WaistGoals),
            () => Assert.Equal(80, user.DesiredWaistCm));
    }

    [Fact]
    public void MealItemSnapshot_WhenLateValidationFails_IsAtomicAndRejectsInvalidServings() {
        var meal = Meal.Create(UserId.New(), Now);
        MealItem item = meal.AddProduct(ProductId.New(), 100);
        item.ApplyProductSnapshot(
            "Original",
            imageUrl: null,
            MeasurementUnit.G,
            baseAmount: 100,
            caloriesPerBase: 120,
            proteinsPerBase: 10,
            fatsPerBase: 5,
            carbsPerBase: 20,
            fiberPerBase: 2,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.ApplyProductSnapshot(
            "Changed",
            "https://example.com/changed.png",
            MeasurementUnit.Pcs,
            baseAmount: 1,
            caloriesPerBase: 200,
            proteinsPerBase: 20,
            fatsPerBase: 10,
            carbsPerBase: 30,
            fiberPerBase: 3,
            alcoholPerBase: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => item.ApplyRecipeSnapshot(
            "Recipe",
            imageUrl: null,
            servings: 0,
            totalCalories: 100,
            totalProteins: 10,
            totalFats: 5,
            totalCarbs: 20,
            totalFiber: 2,
            totalAlcohol: 0));

        Assert.Multiple(
            () => Assert.Equal("Original", item.SnapshotName),
            () => Assert.Null(item.SnapshotImageUrl),
            () => Assert.Equal(MeasurementUnit.G.ToString(), item.SnapshotUnit),
            () => Assert.Equal(100, item.SnapshotBaseAmount),
            () => Assert.Equal(120, item.SnapshotCaloriesPerBase),
            () => Assert.Equal(0, item.SnapshotAlcoholPerBase));
    }

    [Fact]
    public void RefreshTokenRotation_WhenValidationFails_IsAtomic() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "original-hash",
            rememberMe: true,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now);

        Assert.Throws<ArgumentException>(() =>
            session.Rotate(" ", rememberMe: false, Now.AddMinutes(1), TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.Rotate("next-hash", rememberMe: false, DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            new string('h', 513),
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now));

        Assert.Multiple(
            () => Assert.Equal("original-hash", session.RefreshTokenHash),
            () => Assert.True(session.RememberMe),
            () => Assert.Equal(Now, session.LastRotatedAtUtc),
            () => Assert.Null(session.PreviousRefreshTokenHash),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Null(session.ModifiedOnUtc));
    }

    [Fact]
    public void LinkAndUserFacingValues_RejectInvalidInput() {
        Assert.Throws<ArgumentException>(() =>
            UserLessonProgress.Create(UserId.New(), NutritionLessonId.Empty, Now));
        Assert.Throws<ArgumentException>(() => DietologistInvitation.Create(
            UserId.New(), "not-an-email", "hash", Now.AddDays(1), DietologistPermissions.AllEnabled));

        string longName = new('n', DomainConstants.CommentMaxLength + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteProduct.Create(UserId.New(), ProductId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteMeal.Create(UserId.New(), MealId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteRecipe.Create(UserId.New(), RecipeId.New(), longName));
    }

    [Fact]
    public void BoundaryTransitions_HandleOverflowWithoutPartialMutation() {
        var user = User.Create("trial-overflow@example.com", "hash");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartPremiumTrial(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Multiple(
            () => Assert.Null(user.PremiumTrialStartedAtUtc),
            () => Assert.Null(user.PremiumTrialEndsAtUtc));

        BillingWebhookEvent webhookEvent = CreateReceivedWebhookEvent();
        var boundary = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        webhookEvent.MarkFailed(boundary, "failure");
        Assert.Multiple(
            () => Assert.Equal(BillingWebhookEvent.FailedStatus, webhookEvent.Status),
            () => Assert.Equal(1, webhookEvent.AttemptCount),
            () => Assert.Equal("failure", webhookEvent.ErrorMessage),
            () => Assert.Equal(boundary, webhookEvent.NextAttemptAtUtc),
            () => Assert.Equal(boundary, webhookEvent.ModifiedOnUtc));

        BillingWebhookEvent saturatedEvent = CreateReceivedWebhookEvent();
        PropertyInfo attemptCountProperty = typeof(BillingWebhookEvent)
            .GetProperty(nameof(BillingWebhookEvent.AttemptCount))!;
        attemptCountProperty.SetValue(saturatedEvent, int.MaxValue);

        saturatedEvent.MarkFailed(Now.AddMinutes(1), "failure");

        Assert.Equal(int.MaxValue, saturatedEvent.AttemptCount);
    }

    private static BillingWebhookEvent CreateReceivedWebhookEvent() => BillingWebhookEvent.CreateReceived(
        BillingProviderNames.Stripe,
        Guid.NewGuid().ToString("N"),
        "payment.failed",
        externalObjectId: null,
        Now,
        "{}",
        "{}");
}
