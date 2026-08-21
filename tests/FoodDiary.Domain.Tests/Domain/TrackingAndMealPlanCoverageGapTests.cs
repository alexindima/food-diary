using System.Reflection;
using System.Runtime.ExceptionServices;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TrackingAndMealPlanCoverageGapTests {
    [Fact]
    public void MenstrualEpisode_InternalOperations_EnforceRangesAndStatus() {
        DateOnly start = new(2026, 4, 10);

        Assert.Throws<ArgumentException>(() => CreateEpisode(
            profileId: CycleProfileId.Empty, start, end: null, status: MenstrualEpisodeStatus.Inferred));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEpisode(
            profileId: CycleProfileId.New(), start, end: null, status: (MenstrualEpisodeStatus)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEpisode(
            profileId: CycleProfileId.New(), start, end: start.AddDays(-1), status: MenstrualEpisodeStatus.Inferred));

        MenstrualEpisode inferred = CreateEpisode(
            profileId: CycleProfileId.New(), start, end: null, status: MenstrualEpisodeStatus.Inferred);
        InvokeInstance(inferred, "UpdateInferredRange", start.AddDays(2));
        Assert.Equal(start.AddDays(2), inferred.EndDate);
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeInstance(inferred, "UpdateInferredRange", start.AddDays(-1)));
        Assert.Throws<InvalidOperationException>(() => InvokeInstance(inferred, "SetPredictionExclusion", true));

        MenstrualEpisode confirmed = CreateEpisode(
            profileId: CycleProfileId.New(), start, end: null, status: MenstrualEpisodeStatus.Confirmed);
        InvokeInstance(confirmed, "UpdateInferredRange", start.AddDays(3));
        InvokeInstance(confirmed, "SetPredictionExclusion", false);
        Assert.Null(confirmed.EndDate);
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeInstance(confirmed, "UpdateConfirmedRange", start, start.AddDays(-1)));
    }

    [Fact]
    public void CycleConsent_InternalOperations_EnforceIdentityPurposeAndTimeline() {
        DateTime grantedAt = new(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => CreateConsent(CycleProfileId.Empty, CycleConsentPurpose.CycleTracking, grantedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateConsent(CycleProfileId.New(), (CycleConsentPurpose)999, grantedAt));

        CycleConsent consent = CreateConsent(CycleProfileId.New(), CycleConsentPurpose.CycleTracking, grantedAt);
        Assert.False(InvokeInstance<bool>(consent, "Grant", grantedAt.AddMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeInstance(consent, "Revoke", grantedAt.AddMinutes(-1)));
        InvokeInstance(consent, "Revoke", grantedAt.AddMinutes(1));
        InvokeInstance(consent, "Revoke", grantedAt.AddMinutes(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeInstance(consent, "Grant", grantedAt));
        Assert.True(InvokeInstance<bool>(consent, "Grant", grantedAt.AddMinutes(2)));
    }

    [Fact]
    public void MealPlanFactories_RejectInvalidIdentifiersEnumsAndCounts() {
        Assert.Throws<ArgumentException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.New(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeStatic<MealPlanDay>(typeof(MealPlanDay), "Create", MealPlanId.New(), 32));

        Assert.Throws<ArgumentException>(() => CreateMeal(MealPlanDayId.Empty, MealType.Breakfast, RecipeId.New(), 1));
        Assert.Throws<ArgumentException>(() => CreateMeal(MealPlanDayId.New(), MealType.Breakfast, RecipeId.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeal(MealPlanDayId.New(), (MealType)999, RecipeId.New(), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMeal(MealPlanDayId.New(), MealType.Breakfast, RecipeId.New(), 0));
    }

    [Fact]
    public void CyclePredictionRevisionFactory_RejectsInvalidCoreValues() {
        DateTime generatedAt = new(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc);
        DateOnly from = new(2026, 5, 1);

        Assert.Throws<ArgumentException>(() => CreateRevision(CycleProfileId.Empty, generatedAt, from, from, 1, 1, 50));
        Assert.Throws<ArgumentException>(() => CreateRevision(CycleProfileId.New(), generatedAt, from.AddDays(1), from, 1, 1, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRevision(CycleProfileId.New(), generatedAt, from, from, -1, 1, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRevision(CycleProfileId.New(), generatedAt, from, from, 1, -1, 50));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRevision(CycleProfileId.New(), generatedAt, from, from, 1, 1, 101));
        Assert.Throws<ArgumentNullException>(() => CreateRevision(CycleProfileId.New(), generatedAt, from, from, 1, 1, 50, reasonCodes: null));
    }

    private static MenstrualEpisode CreateEpisode(
        CycleProfileId profileId,
        DateOnly start,
        DateOnly? end,
        MenstrualEpisodeStatus status) =>
        InvokeStatic<MenstrualEpisode>(typeof(MenstrualEpisode), "Create", profileId, start, end, status, false);

    private static CycleConsent CreateConsent(CycleProfileId profileId, CycleConsentPurpose purpose, DateTime grantedAt) =>
        InvokeStatic<CycleConsent>(typeof(CycleConsent), "Create", profileId, purpose, grantedAt);

    private static MealPlanMeal CreateMeal(MealPlanDayId dayId, MealType mealType, RecipeId recipeId, int servings) =>
        InvokeStatic<MealPlanMeal>(typeof(MealPlanMeal), "Create", dayId, mealType, recipeId, servings);

    private static CyclePredictionRevision CreateRevision(
        CycleProfileId profileId,
        DateTime generatedAt,
        DateOnly? from,
        DateOnly? to,
        int completedCycles,
        int calibrationSamples,
        double? coverage,
        IReadOnlyCollection<string>? reasonCodes = null) =>
        InvokeStatic<CyclePredictionRevision>(
            typeof(CyclePredictionRevision),
            "Create",
            profileId,
            generatedAt,
            from,
            to,
            "high",
            "enough",
            "stable",
            completedCycles,
            calibrationSamples,
            coverage,
            1d,
            reasonCodes!,
            "v1");

    private static T InvokeStatic<T>(Type type, string methodName, params object?[] arguments) =>
        Invoke<T>(type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!, instance: null, arguments);

    private static void InvokeInstance(object instance, string methodName, params object?[] arguments) =>
        _ = Invoke<object?>(instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!, instance, arguments);

    private static T InvokeInstance<T>(object instance, string methodName, params object?[] arguments) =>
        Invoke<T>(instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!, instance, arguments);

    private static T Invoke<T>(MethodInfo method, object? instance, object?[] arguments) {
        try {
            return (T)method.Invoke(instance, arguments)!;
        } catch (TargetInvocationException exception) when (exception.InnerException is not null) {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
