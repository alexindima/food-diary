using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;
using FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Modules.Dietologist.Contracts.Common;
using FoodDiary.Modules.Dietologist.Contracts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Services;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;
using FoodDiary.Modules.Usda.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RetiredDomainAssemblyTests {
    [Theory]
    [InlineData(typeof(IFavoriteMealSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(IFavoriteProductSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(IFavoriteRecipeSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FavoriteMealSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FavoriteProductSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FavoriteRecipeSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(ReadRecentProductsQuery), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(IRecentItemUsageRecorder), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(RecentProductUsage), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(RecentRecipeUsage), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(RecordPremiumConversionCommand), "FoodDiary.Modules.Marketing.Contracts")]
    [InlineData(typeof(IDietologistDashboardAccessService), "FoodDiary.Modules.Dietologist.Contracts")]
    [InlineData(typeof(DietologistPermissionsReadModel), "FoodDiary.Modules.Dietologist.Contracts")]
    [InlineData(typeof(IUsdaFoodSearchService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(SearchUsdaFoodsQuery), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(IUsdaProductLinkService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(IUsdaMealNutritionReadService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(UsdaMealProductNutritionReadModel), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(GetAiPromptRevisionsQuery), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(UpsertAiPromptCommand), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(GetCompletedFoodRecognitionQuery), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(IAchievementEvaluationOutbox), "FoodDiary.Modules.Gamification.Contracts")]
    [InlineData(typeof(UserCalorieSchedule), "FoodDiary.Modules.Users.Domain.Contracts")]
    [InlineData(typeof(UserPreferenceUpdate), "FoodDiary.Modules.Users.Domain.Contracts")]
    [InlineData(typeof(NutritionLessonId), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(LessonCategory), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(LessonDifficulty), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(INotificationWriter), "FoodDiary.Modules.Notifications.Contracts")]
    [InlineData(typeof(IEmailSender), "FoodDiary.Modules.Identity.Contracts")]
    [InlineData(typeof(IImpersonationTokenIssuer), "FoodDiary.Modules.Identity.Contracts")]
    [InlineData(typeof(ReadWeightEntriesQuery), "FoodDiary.Modules.BodyMetrics.Contracts")]
    [InlineData(typeof(ReadWaistEntriesQuery), "FoodDiary.Modules.BodyMetrics.Contracts")]
    [InlineData(typeof(ProductErrors), "FoodDiary.Modules.Products.Contracts")]
    [InlineData(typeof(RecipeErrors), "FoodDiary.Modules.Recipes.Contracts")]
    [InlineData(typeof(CycleErrors), "FoodDiary.Modules.Cycles.Contracts")]
    [InlineData(typeof(IImageAssetCleanupService), "FoodDiary.Modules.Images.Service.Contracts")]
    [InlineData(typeof(IImageAssetOwnershipService), "FoodDiary.Modules.Images.Service.Contracts")]
    public void ConsumerValueAndCapability_HasNarrowAssemblyOwner(Type type, string assembly) {
        Assert.Equal(assembly, type.Assembly.GetName().Name);
    }

    [Theory]
    [InlineData(typeof(RoleNames), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/Enums", "FoodDiary.Modules.Users.Domain.Contracts.Enums")]
    [InlineData(typeof(MeasurementUnit), "FoodDiary.Modules.Products.Domain.Contracts", "Modules/Products/Domain.Contracts/Enums", "FoodDiary.Modules.Products.Domain.Contracts.Enums")]
    [InlineData(typeof(FoodQualityScore), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Modules.Products.FoodQuality.ValueObjects")]
    [InlineData(typeof(FoodQualityGrade), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Modules.Products.FoodQuality.ValueObjects")]
    [InlineData(typeof(HealthAreaScore), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Modules.Usda.Domain.ValueObjects")]
    [InlineData(typeof(HealthAreaGrade), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Modules.Usda.Domain.ValueObjects")]
    [InlineData(typeof(HealthAreaScores), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Modules.Usda.Domain.ValueObjects")]
    [InlineData(typeof(DesiredWeightKg), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Modules.Users.Domain.Contracts.ValueObjects")]
    [InlineData(typeof(DesiredWaistCm), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Modules.Users.Domain.Contracts.ValueObjects")]
    [InlineData(typeof(LanguageCode), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Modules.Users.Domain.Contracts.ValueObjects")]
    [InlineData(typeof(EmailAddress), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
    [InlineData(typeof(Visibility), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
    public void RelocatedTypes_HaveExactlyOneApprovedOwner(Type type, string assembly, string folder, string expectedNamespace) {
        Assert.Equal(assembly, type.Assembly.GetName().Name);
        Assert.Equal(expectedNamespace, type.Namespace);
        string expected = Path.GetFullPath(ArchitectureTestPaths.FromRoot(folder, type.Name + ".cs"));
        string[] declarations = [.. SourceScanner.SourceFiles([
                ArchitectureTestPaths.FromRoot("Modules"),
                ArchitectureTestPaths.FromRoot("Shared"),
                ArchitectureTestPaths.FromRoot("FoodDiary.Domain"),
            ])
            .Where(path => string.Equals(Path.GetFileName(path), type.Name + ".cs", StringComparison.Ordinal))];
        Assert.Equal([expected], declarations);
    }

    [Theory]
    [InlineData("FoodDiary.Domain", "FoodDiary.Domain/FoodDiary.Domain.csproj")]
    [InlineData("FoodDiary.Nutrition.Domain", "Shared/FoodDiary.Nutrition.Domain/FoodDiary.Nutrition.Domain.csproj")]
    public void RetiredAssemblies_AreAbsentFromProductionAndTestGraphs(string name, string path) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(path)));
        Assert.DoesNotContain(name, ProjectReferenceReader.ReadProductionProjectNames(), StringComparer.Ordinal);
        foreach (string[] references in ProjectReferenceReader.ReadProductionProjectReferences().Values
                     .Concat(ProjectReferenceReader.ReadTestProjectReferences().Values)) {
            Assert.DoesNotContain(name, references, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void ProductContracts_ContainIdentityClassificationMeasurementAndQuantityWithoutAggregateDependencies() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Products/Domain.Contracts");
        Assert.Equal(["Enums/MeasurementUnit.cs", "Enums/ProductType.cs", "ValueObjects/Ids/ProductId.cs", "ValueObjects/ProductUnitQuantity.cs"],
            SourceScanner.SourceFiles(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')), StringComparer.Ordinal);
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Products/Domain.Contracts/FoodDiary.Modules.Products.Domain.Contracts.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void FocusedNutritionTests_BelongToProductsWithoutCentralDuplicates() {
        string focused = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/FoodQualityScoreTests.cs"));
        Assert.Contains("FoodQualityScore_Calculate_WithZeroCalories_ReturnsYellow50", focused, StringComparison.Ordinal);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/ValueObjects/NutritionContractTests.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "tests/FoodDiary.Nutrition.Domain.Tests/FoodDiary.Nutrition.Domain.Tests.csproj")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "tests/FoodDiary.Domain.Tests/FoodDiary.Domain.Tests.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests/FoodQualityScoreBoundaryTests.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules/Usda/tests/FoodDiary.Modules.Usda.Domain.Tests/HealthAreaScoreBoundaryTests.cs")));
    }
}
