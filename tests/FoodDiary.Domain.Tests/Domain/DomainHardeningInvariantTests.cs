using System.Reflection;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DomainHardeningInvariantTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Product_Create_WithNonFiniteBaseAmount_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            "Apple",
            MeasurementUnit.G,
            value,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0));
    }

    [Fact]
    public void WearableSyncEntry_RejectsNonFiniteValuesAndNormalizesDateToUtc() {
        var unspecified = new DateTime(2026, 4, 28, 17, 45, 0, DateTimeKind.Unspecified);
        var entry = WearableSyncEntry.Create(
            UserId.New(),
            WearableProvider.Fitbit,
            WearableDataType.Steps,
            unspecified,
            10_000);

        Assert.Multiple(
            () => Assert.Equal(DateTimeKind.Utc, entry.Date.Kind),
            () => Assert.Equal(new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), entry.Date),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateValue(double.NaN)),
            () => Assert.Equal(10_000, entry.Value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.1)]
    public void FastingTelemetryEvent_Create_WithInvalidActualDuration_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.completed", Now, actualDurationHours: value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void MealPlan_Create_WithInvalidTargetCalories_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealPlan.CreateCurated(
                "Plan",
                description: null,
                DietType.Balanced,
                durationDays: 7,
                targetCaloriesPerDay: value));
    }

    [Fact]
    public void OpenFoodFactsProduct_RejectsPersistenceOverflowAndInvalidNutritionAtomically() {
        OpenFoodFactsProduct product = CreateOpenFoodFactsProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            new string('n', 513),
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: 10,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            "Changed",
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: double.NaN,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));

        Assert.Multiple(
            () => Assert.Equal("Milk", product.Name),
            () => Assert.Equal(64, product.CaloriesPer100G),
            () => Assert.Equal(1, product.SearchHitCount));
    }

    [Fact]
    public void MealAiData_PublicConstructorValidatesAndFailedSessionAdditionIsAtomic() {
        ConstructorInfo[] constructors = typeof(MealAiItemData).GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var meal = Meal.Create(UserId.New(), Now);

        Assert.Single(constructors);
        Assert.Throws<ArgumentOutOfRangeException>(() => new MealAiItemData(
            "Apple",
            nameLocal: null,
            amount: double.NaN,
            "g",
            calories: 52,
            proteins: 0.3,
            fats: 0.2,
            carbs: 14,
            fiber: 2.4,
            alcohol: 0));
        Assert.Throws<ArgumentException>(() => meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            Now,
            notes: null,
            [null!]));
        Assert.Empty(meal.AiSessions);
    }

    [Fact]
    public void CycleProfile_ConfirmPeriodStart_RejectsOverlappingConfirmedRange() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode first = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(first.Id, start, start.AddDays(5));

        Assert.Throws<ArgumentException>(() => profile.ConfirmPeriodStart(start.AddDays(3)));
        Assert.Single(profile.MenstrualEpisodes);
    }

    [Fact]
    public void CycleProfile_Reconciliation_DoesNotInferRangeOverlappingConfirmedEpisodeEnd() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode confirmed = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(confirmed.Id, start, start.AddDays(10));

        profile.UpsertBleedingEntry(
            start.AddDays(8),
            BleedingType.Bleeding,
            CycleFlowLevel.Medium,
            painImpact: null,
            notes: null);

        MenstrualEpisode episode = Assert.Single(profile.MenstrualEpisodes);
        Assert.Equal(MenstrualEpisodeStatus.Confirmed, episode.Status);
    }

    [Fact]
    public void CycleProfile_RecordPredictionRevision_NormalizesValidValues() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        profile.RecordPredictionRevision(
            Now,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 3),
            " high ",
            " sufficient ",
            " stable ",
            completedCycleCount: 4,
            calibrationSampleCount: 3,
            historicalCoveragePercent: 95.5,
            meanAbsoluteErrorDays: 1.2,
            reasonCodes: [" regular ", "enough-data"],
            algorithmVersion: " v2 ");

        CyclePredictionRevision revision = Assert.Single(profile.PredictionRevisions);
        Assert.Multiple(
            () => Assert.Equal("high", revision.Confidence),
            () => Assert.Equal("regular|enough-data", revision.ReasonCodes),
            () => Assert.Equal("v2", revision.AlgorithmVersion));
    }

    [Theory]
    [InlineData(-1, 0, 50, 1)]
    [InlineData(0, -1, 50, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 101, 1)]
    [InlineData(0, 0, 50, -1)]
    public void CycleProfile_RecordPredictionRevision_RejectsInvalidMetrics(
        int completedCycles,
        int samples,
        double coverage,
        double errorDays) {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => profile.RecordPredictionRevision(
            Now,
            nextPeriodStartFrom: null,
            nextPeriodStartTo: null,
            "high",
            "sufficient",
            "stable",
            completedCycles,
            samples,
            coverage,
            errorDays,
            [],
            "v2"));
        Assert.Empty(profile.PredictionRevisions);
    }

    [Fact]
    public void BillingSubscription_InvalidSnapshotDoesNotPartiallyMutateState() {
        var subscription = BillingSubscription.CreatePending(
            UserId.New(),
            BillingProviderNames.Stripe,
            "customer_1",
            "price_1",
            "monthly");

        Assert.Throws<ArgumentException>(() => subscription.ApplyProviderSnapshot(
            BillingProviderNames.Paddle,
            "sub_changed",
            "pm_changed",
            "price_changed",
            "annual",
            "active",
            Now.AddDays(2),
            Now.AddDays(1),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            "evt_1",
            Now));

        Assert.Multiple(
            () => Assert.Equal(BillingProviderNames.Stripe, subscription.Provider),
            () => Assert.Null(subscription.ExternalSubscriptionId),
            () => Assert.Equal("price_1", subscription.ExternalPriceId),
            () => Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status));
    }

    [Fact]
    public void BillingPayment_InvalidProviderResultDoesNotPartiallyMutateState() {
        BillingPayment payment = CreateBillingPayment();

        Assert.Throws<ArgumentException>(() => payment.ApplyProviderResult(
            billingSubscriptionId: Guid.NewGuid(),
            externalCustomerId: "changed",
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            status: "active",
            kind: BillingPaymentKinds.Webhook,
            amount: null,
            currency: null,
            currentPeriodStartUtc: Now.AddDays(2),
            currentPeriodEndUtc: Now.AddDays(1),
            webhookEventId: null,
            providerMetadataJson: null));

        Assert.Multiple(
            () => Assert.Null(payment.BillingSubscriptionId),
            () => Assert.Equal("customer_1", payment.ExternalCustomerId),
            () => Assert.Null(payment.CurrentPeriodStartUtc));
    }

    [Fact]
    public void User_BirthDateIsStoredAsUtcDateAndGoogleLinkingIsAtomic() {
        var user = User.Create("user@example.com", "hashed-password");
        var localBirthDate = new DateTime(1990, 5, 12, 18, 30, 0, DateTimeKind.Local);
        user.UpdatePersonalInfo(birthDate: localBirthDate);
        user.LinkGoogleIdentity("issuer", "subject");
        DateTime storedBirthDate = user.BirthDate.GetValueOrDefault();

        Assert.Throws<ArgumentException>(() => user.LinkGoogleIdentity("changed", " "));

        Assert.Multiple(
            () => Assert.True(user.BirthDate.HasValue),
            () => Assert.Equal(DateTimeKind.Utc, storedBirthDate.Kind),
            () => Assert.Equal(localBirthDate.ToUniversalTime().Date, storedBirthDate),
            () => Assert.Equal("issuer", user.GoogleIssuer),
            () => Assert.Equal("subject", user.GoogleSubject));
    }

    private static OpenFoodFactsProduct CreateOpenFoodFactsProduct() {
        return OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            "Brand",
            "Dairy",
            imageUrl: null,
            caloriesPer100G: 64,
            proteinsPer100G: 3.2,
            fatsPer100G: 3.5,
            carbsPer100G: 4.8,
            fiberPer100G: 0,
            Now);
    }

    private static BillingPayment CreateBillingPayment() {
        return BillingPayment.Create(
            UserId.New(),
            billingSubscriptionId: null,
            BillingProviderNames.Stripe,
            "payment_1",
            "customer_1",
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            "active",
            BillingPaymentKinds.Webhook,
            amount: null,
            currency: null,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson: null);
    }
}
