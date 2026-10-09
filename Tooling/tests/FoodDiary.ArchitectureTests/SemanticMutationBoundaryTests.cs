using System.Reflection;
using System.Text.RegularExpressions;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.ValueObjects;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Modules.Exercises.Domain.ValueObjects;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.ValueObjects;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;
using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Modules.RecentItems.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class SemanticMutationBoundaryTests {
    [Theory]
    [InlineData("Modules/Users/Application", @"\.(?:StartWeightGoal|StartWaistGoal|CancelWeightGoal|CancelWaistGoal)\s*\(")]
    [InlineData("Modules/BodyMetrics/Application", @"\b(?:WeightEntry|WaistEntry)\.(?:Create|CreateForDay)\s*\(|\.UpdateDetails\s*\(")]
    [InlineData("Modules/Fasting/Application", @"\bFastingPlan\.(?:CreateIntermittent|CreateExtended|CreateCyclic)\s*\(|\.ScheduleNextCyclicPhase\s*\(")]
    [InlineData("Modules/Exercises/Application", @"\bExerciseEntry\.Create\s*\(")]
    [InlineData("Modules/MealPlanning/Application/ShoppingLists", @"\.(?:AddItem|AddMealPlanSource)\s*\(")]
    [InlineData("Modules/Favorites/Application/FavoriteProducts", @"\bFavoriteProduct\.Create\s*\(|\.UpdatePreferredPortionAmount\s*\(")]
    [InlineData("Modules/ContentReports/Application", @"\bContentReport\.Create\s*\(")]
    [InlineData("Modules/Billing/Application", @"\bBillingPayment\.Create\s*\(|\.ApplyProviderResult\s*\(")]
    [InlineData("Modules/Products/Application", @"\bProduct\.Create\s*\(|\.(?:UpdateCoreIdentity|UpdateDescriptiveIdentity|UpdateMedia|UpdateMeasurementAndNutrition)\s*\(")]
    [InlineData("Modules/Recipes/Application/Commands/UpdateRecipe", @"\.(?:UpdateIdentity|UpdateMedia)\s*\(")]
    [InlineData("Modules/Users/Application", @"\.UpdatePersonalInfo\s*\(")]
    [InlineData("Modules/Wearables/Application", @"\bWearableSyncEntry\.Create\s*\(|\.UpdateValue\s*\(")]
    [InlineData("Modules/MealPlanning/Application/MealPlans", @"\bMealPlan\.(?:CreateCurated|CreateForUser)\s*\(|\.(?:AddDay|AddMeal|UpdateCatalogDetails)\s*\(")]
    [InlineData("Modules/Gamification/Application", @"\bAchievementDefinition\.Create\s*\(|\bdefinition\.Update\s*\(")]
    [InlineData("Modules/Ai/Infrastructure/Persistence", @"\bAiUsage\.Create\s*\(")]
    [InlineData("Modules/Cycles/Application", @"\.RecordPredictionRevision\s*\(")]
    [InlineData("Modules/Users/Infrastructure/Persistence/Users", @"\bUser\.CalculateBmr\s*\(")]
    public void OwningProductionMutations_DoNotReturnToScalarCompatibilityAdapters(string scope, string forbiddenCall) {
        string[] sources = [.. SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot(scope))];
        Assert.NotEmpty(sources);
        var pattern = new Regex(forbiddenCall, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        Assert.All(sources, path => Assert.False(pattern.IsMatch(File.ReadAllText(path)), path));
    }

    [Theory]
    [InlineData(typeof(User), "StartWeightGoalWithMeasurements", typeof(MeasuredWeightKg))]
    [InlineData(typeof(User), "StartWaistGoalWithMeasurements", typeof(MeasuredWaistCm))]
    [InlineData(typeof(User), "UpdatePersonalInfoChanges", typeof(UserPersonalInfoChanges))]
    [InlineData(typeof(User), "CalculateBmrFromProfile", typeof(BmrCalculationInput))]
    [InlineData(typeof(WeightEntry), "CreateWithMeasurement", typeof(MeasuredWeightKg))]
    [InlineData(typeof(WaistEntry), "CreateWithMeasurement", typeof(MeasuredWaistCm))]
    [InlineData(typeof(FastingPlan), "CreateWithSettings", typeof(FastingPlanSettings))]
    [InlineData(typeof(ExerciseEntry), "CreateWithValues", typeof(ExerciseDuration))]
    [InlineData(typeof(ExerciseEntry), "CreateWithValues", typeof(BurnedEnergy))]
    [InlineData(typeof(ShoppingList), "AddItemWithQuantity", typeof(ShoppingQuantity))]
    [InlineData(typeof(ShoppingListItem), "AddMealPlanSourceWithQuantity", typeof(ShoppingSourceQuantity))]
    [InlineData(typeof(FavoriteProduct), "CreateWithPreferredQuantity", typeof(PreferredProductQuantity))]
    [InlineData(typeof(ContentReport), "CreateWithTarget", typeof(ReportTarget))]
    [InlineData(typeof(RecentItem), "CreateWithReference", typeof(RecentItemReference))]
    [InlineData(typeof(BillingPayment), "CreateWithFinancials", typeof(BillingPaymentFinancials))]
    public void SemanticMutation_RequiresTheOwnerValue(Type owner, string methodName, Type valueType) {
        MethodInfo method = Assert.Single(owner.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly),
            candidate => string.Equals(candidate.Name, methodName, StringComparison.Ordinal));
        Assert.Contains(method.GetParameters(), parameter => parameter.ParameterType == valueType);
    }

    [Theory]
    [InlineData(typeof(DesiredWeightKg))]
    [InlineData(typeof(DesiredWaistCm))]
    [InlineData(typeof(MeasuredWeightKg))]
    [InlineData(typeof(MeasuredWaistCm))]
    [InlineData(typeof(ShoppingQuantity))]
    [InlineData(typeof(ShoppingSourceQuantity))]
    [InlineData(typeof(PreferredProductQuantity))]
    [InlineData(typeof(ExerciseDuration))]
    [InlineData(typeof(BurnedEnergy))]
    [InlineData(typeof(ReportTarget))]
    [InlineData(typeof(RecentItemReference))]
    [InlineData(typeof(BillingPaymentFinancials))]
    [InlineData(typeof(BmrCalculationInput))]
    [InlineData(typeof(PlanDurationDays))]
    [InlineData(typeof(PlanDayNumber))]
    [InlineData(typeof(PlannedServings))]
    [InlineData(typeof(FieldChange<string>))]
    [InlineData(typeof(ProductMeasurementBasis))]
    [InlineData(typeof(ProductDefaultPortion))]
    [InlineData(typeof(NotificationIntent))]
    [InlineData(typeof(RecommendationCommentTarget))]
    public void ValidatedValue_HasNoDefaultOrMutableEscape(Type valueType) {
        Assert.Multiple(() => {
            Assert.False(valueType.IsValueType);
            Assert.True(valueType.IsSealed);
            Assert.Empty(valueType.GetConstructors());
            Assert.DoesNotContain(valueType.GetProperties(), property => property.SetMethod?.IsPublic == true);
        });
    }

    [Fact]
    public void FastingModes_AreClosedToExternalDerivation() {
        ConstructorInfo[] constructors = typeof(FastingPlanSettings).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotEmpty(constructors);
        Assert.All(constructors, constructor => Assert.True(constructor.IsFamilyAndAssembly));
    }

    [Fact]
    public void GoalPatch_KeepsDesiredValuesDistinctFromMeasuredValues() {
        Assert.Multiple(
            () => Assert.Equal(typeof(DesiredWeightKg), typeof(UserGoalUpdate).GetProperty(nameof(UserGoalUpdate.DesiredWeightKg))!.PropertyType),
            () => Assert.Equal(typeof(DesiredWaistCm), typeof(UserGoalUpdate).GetProperty(nameof(UserGoalUpdate.DesiredWaistCm))!.PropertyType));
    }
}
