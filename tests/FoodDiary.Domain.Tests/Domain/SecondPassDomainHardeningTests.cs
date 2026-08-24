using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class SecondPassDomainHardeningTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void HealthAreaScore_RejectsScoreOutsidePercentageRange(int score) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HealthAreaScore(score, HealthAreaGrade.Unknown));
    }

    [Fact]
    public void CompositeUpdates_WhenLateValidationFails_AreAtomic() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 1);
        Product product = CreateProduct();
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.Update(new RecipeUpdate(
            Name: "Changed",
            ImageUrl: new string('x', 2049))));
        Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateIdentity(new ProductIdentityUpdate(
            Name: "Changed",
            Description: new string('x', 2049))));
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.TryingToConceive,
            AverageCycleLength: 28,
            AveragePeriodLength: 99,
            LutealLength: 14,
            IsRegular: null,
            IsOnboardingComplete: null,
            ShowFertilityEstimates: null,
            DiscreetNotifications: null,
            Notes: null)));

        Assert.Multiple(
            () => Assert.Equal("Original", recipe.Name),
            () => Assert.Null(recipe.ModifiedOnUtc),
            () => Assert.Equal("Product", product.Name),
            () => Assert.Null(product.ModifiedOnUtc),
            () => Assert.Equal(CycleTrackingMode.PeriodTracking, profile.Mode),
            () => Assert.Equal(5, profile.AveragePeriodLength),
            () => Assert.Null(profile.ModifiedOnUtc));
    }

    [Fact]
    public void EntityUpdates_WhenLateValidationFails_AreAtomic() {
        var originalUserId = UserId.New();
        var subscription = WebPushSubscription.Create(originalUserId, "https://push.example", "p256", "auth");
        var template = RecommendationTemplate.Create(UserId.New(), "Original", "Original text");
        var definition = AchievementDefinition.Create(
            "first_meal", "nutrition", AchievementMetric.TotalMeals, 1,
            "Первая еда", "First meal", "Описание", "Description", "meal", 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.Refresh(
            UserId.New(),
            "changed",
            new string('x', 513)));
        Assert.Throws<ArgumentOutOfRangeException>(() => template.Update("Changed", new string('x', 2001)));
        Assert.Throws<ArgumentOutOfRangeException>(() => definition.Update(
            category: "changed",
            metric: AchievementMetric.TotalMeals,
            threshold: 2,
            titleRu: new string('x', 161),
            titleEn: "Changed",
            descriptionRu: "Changed",
            descriptionEn: "Changed",
            icon: "icon",
            sortOrder: 1,
            isActive: true));

        Assert.Multiple(
            () => Assert.Equal(originalUserId, subscription.UserId),
            () => Assert.Equal("p256", subscription.P256Dh),
            () => Assert.Equal("Original", template.Name),
            () => Assert.Equal("nutrition", definition.Category),
            () => Assert.Equal(1, definition.Version));
    }

    [Fact]
    public void StorageBoundValues_AreValidatedAndUtcNormalized() {
        var localExpiry = DateTime.SpecifyKind(DateTime.Now.AddHours(1), DateTimeKind.Local);
        var wearable = WearableConnection.Create(
            UserId.New(), WearableProvider.Fitbit, " external ", " access ", " refresh ", localExpiry);

        Assert.Multiple(
            () => Assert.Equal("external", wearable.ExternalUserId),
            () => Assert.Equal("access", wearable.AccessToken),
            () => Assert.Equal(DateTimeKind.Utc, wearable.TokenExpiresAtUtc!.Value.Kind));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: new string('x', 257),
            accessToken: "access",
            refreshToken: null,
            tokenExpiresAtUtc: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: "external",
            accessToken: new string('x', 8193),
            refreshToken: null,
            tokenExpiresAtUtc: null));
        Assert.Throws<ArgumentOutOfRangeException>(() => WearableConnection.Create(
            userId: UserId.New(),
            provider: WearableProvider.Fitbit,
            externalUserId: "external",
            accessToken: "access",
            refreshToken: null,
            tokenExpiresAtUtc: new DateTime(year: 2026, month: 1, day: 1)));

        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), new string('x', 257), "hash", DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled));
        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), "diet@example.com", new string('x', 257), DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled));
        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), "diet@example.com", "hash", new DateTime(2026, 1, 1), DietologistPermissions.AllEnabled));

        var report = ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Reason");
        Assert.Throws<ArgumentOutOfRangeException>(() => report.MarkReviewed(UserId.New(), new string('x', 2001)));
        Assert.Equal(ReportStatus.Pending, report.Status);
    }

    [Fact]
    public void JsonBackedValues_RejectInvalidJson() {
        var user = User.Create("json@example.com", "hash");

        Assert.Throws<ArgumentException>(() => user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "{invalid")));
        Assert.Throws<ArgumentException>(() => BillingWebhookEvent.CreateReceived(
            BillingProviderNames.Stripe,
            "event",
            "payment",
            externalObjectId: null,
            DateTime.UtcNow,
            "{invalid",
            "{}"));
        Assert.Throws<ArgumentException>(() => CreatePayment(providerMetadataJson: "{invalid"));
    }

    [Fact]
    public void BillingPayment_ValidatesStoragePrecisionAndCurrency() {
        BillingPayment payment = CreatePayment(amount: 12.34m, currency: " usd ");

        Assert.Equal("USD", payment.Currency);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(amount: 12.345m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(amount: 10_000_000_000_000_000m));
        Assert.Throws<ArgumentException>(() => CreatePayment(currency: "US1"));
        Assert.Throws<ArgumentException>(() => CreatePayment(currency: "US"));
    }

    [Fact]
    public void MealPlanAndRecipe_RejectOutOfAggregateRangeAndDirectCycles() {
        var plan = MealPlan.CreateForUser(
            userId: UserId.New(),
            name: "Week",
            description: null,
            dietType: DietType.Balanced,
            durationDays: 7,
            targetCaloriesPerDay: null);
        var recipe = Recipe.Create(UserId.New(), "Recipe", servings: 1);
        RecipeStep step = recipe.AddStep(1, "Mix");

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.AddDay(8));
        Assert.Throws<ArgumentException>(() => step.AddNestedRecipeIngredient(recipe.Id, 1));
        Assert.Empty(plan.Days);
        Assert.Empty(step.Ingredients);
    }

    [Fact]
    public void FastingTransitions_EnforceStatusAndChronology() {
        DateTime startedAt = DateTime.UtcNow;
        var active = FastingOccurrence.Create(
            FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, startedAt, 1);
        var scheduled = FastingOccurrence.Schedule(
            FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, startedAt.AddDays(1), 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => active.Complete(startedAt.AddSeconds(-1)));
        Assert.Throws<InvalidOperationException>(() => scheduled.Complete(startedAt.AddDays(2)));
        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Active, active.Status),
            () => Assert.Null(active.EndedAtUtc),
            () => Assert.Equal(FastingOccurrenceStatus.Scheduled, scheduled.Status));
    }

    [Fact]
    public void ScoreValueObjects_RejectIncoherentOrInvalidInputs() {
        Assert.Throws<ArgumentException>(() => new FoodQualityScore(80, FoodQualityGrade.Red));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FoodQualityScore(101, FoodQualityGrade.Green));
        Assert.Throws<ArgumentException>(() => new HealthAreaScore(80, HealthAreaGrade.Low));
        Assert.Throws<ArgumentOutOfRangeException>(() => FoodQualityScore.Calculate(
            double.NaN, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FoodQualityScore.Calculate(
            100, -1, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => HealthAreaScores.Calculate(
            new Dictionary<int, double> { [1092] = double.PositiveInfinity },
            new Dictionary<int, double> { [1092] = 100 }));
    }

    private static Product CreateProduct() => Product.Create(
        UserId.New(),
        "Product",
        MeasurementUnit.G,
        100,
        defaultPortionAmount: null,
        caloriesPerBase: 100,
        proteinsPerBase: 10,
        fatsPerBase: 5,
        carbsPerBase: 10,
        fiberPerBase: 2,
        alcoholPerBase: 0);

    private static BillingPayment CreatePayment(
        decimal? amount = null,
        string? currency = null,
        string? providerMetadataJson = null) => BillingPayment.Create(
            UserId.New(),
            billingSubscriptionId: null,
            BillingProviderNames.Stripe,
            "payment",
            externalCustomerId: null,
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            status: "active",
            BillingPaymentKinds.Webhook,
            amount,
            currency,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson);
}
