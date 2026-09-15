namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RetiredDomainAssemblyTests {
    [Theory]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common.IFavoriteMealSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Common.IFavoriteProductSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Common.IFavoriteRecipeSourceReadService), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models.FavoriteMealSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models.FavoriteProductSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models.FavoriteRecipeSourceModel), "FoodDiary.Modules.Favorites.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts.ReadRecentProductsQuery), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Common.IRecentItemUsageRecorder), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Common.RecentProductUsage), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.RecentItems.Contracts.Common.RecentRecipeUsage), "FoodDiary.Modules.RecentItems.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion.RecordPremiumConversionCommand), "FoodDiary.Modules.Marketing.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Dietologist.Contracts.Common.IDietologistDashboardAccessService), "FoodDiary.Modules.Dietologist.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Dietologist.Contracts.Models.DietologistPermissionsReadModel), "FoodDiary.Modules.Dietologist.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Common.IUsdaFoodSearchService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods.SearchUsdaFoodsQuery), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Common.IUsdaProductLinkService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Common.IUsdaMealNutritionReadService), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Contracts.Models.UsdaMealProductNutritionReadModel), "FoodDiary.Modules.Usda.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions.GetAiPromptRevisionsQuery), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt.UpsertAiPromptCommand), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Ai.Contracts.Queries.GetCompletedFoodRecognition.GetCompletedFoodRecognitionQuery), "FoodDiary.Modules.Ai.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Gamification.Contracts.Achievements.Common.IAchievementEvaluationOutbox), "FoodDiary.Modules.Gamification.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.UserCalorieSchedule), "FoodDiary.Modules.Users.Domain.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.UserPreferenceUpdate), "FoodDiary.Modules.Users.Domain.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids.NutritionLessonId), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Domain.Contracts.Enums.LessonCategory), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Lessons.Domain.Contracts.Enums.LessonDifficulty), "FoodDiary.Modules.Lessons.Domain.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Notifications.Contracts.Common.INotificationWriter), "FoodDiary.Modules.Notifications.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Common.IEmailSender), "FoodDiary.Modules.Identity.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Identity.Contracts.Authentication.Services.IImpersonationTokenIssuer), "FoodDiary.Modules.Identity.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQuery), "FoodDiary.Modules.BodyMetrics.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQuery), "FoodDiary.Modules.BodyMetrics.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Products.Contracts.Common.ProductErrors), "FoodDiary.Modules.Products.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Recipes.Contracts.Common.RecipeErrors), "FoodDiary.Modules.Recipes.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Contracts.Common.CycleErrors), "FoodDiary.Modules.Cycles.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Images.Service.Contracts.Common.IImageAssetCleanupService), "FoodDiary.Modules.Images.Service.Contracts")]
    [InlineData(typeof(FoodDiary.Modules.Images.Service.Contracts.Common.IImageAssetOwnershipService), "FoodDiary.Modules.Images.Service.Contracts")]
    public void ConsumerValueAndCapability_HasNarrowAssemblyOwner(Type type, string assembly) {
        Assert.Equal(assembly, type.Assembly.GetName().Name);
    }

    [Theory]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.Enums.RoleNames), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/Enums", "FoodDiary.Domain.Enums")]
    [InlineData(typeof(FoodDiary.Modules.Products.Domain.Contracts.Enums.MeasurementUnit), "FoodDiary.Modules.Products.Domain.Contracts", "Modules/Products/Domain.Contracts/Enums", "FoodDiary.Modules.Products.Domain.Contracts.Enums")]
    [InlineData(typeof(FoodDiary.Modules.Products.FoodQuality.ValueObjects.FoodQualityScore), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Products.FoodQuality.ValueObjects.FoodQualityGrade), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Domain.ValueObjects.HealthAreaScore), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Domain.ValueObjects.HealthAreaGrade), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Usda.Domain.ValueObjects.HealthAreaScores), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.DesiredWeightKg), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.DesiredWaistCm), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.LanguageCode), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.Primitives.EmailAddress), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
    [InlineData(typeof(FoodDiary.Domain.Primitives.Visibility), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
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
    public void ProductContracts_ContainIdentityClassificationAndMeasurementWithoutAggregateDependencies() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Products/Domain.Contracts");
        Assert.Equal(["Enums/MeasurementUnit.cs", "Enums/ProductType.cs", "ValueObjects/Ids/ProductId.cs"],
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
        string central = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "tests/FoodDiary.Domain.Tests/Domain/ValueObjects/AdditionalValueObjectsInvariantTests.cs"));
        Assert.DoesNotContain("FoodQualityScore_Calculate_", central, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthAreaScores_Calculate_", central, StringComparison.Ordinal);
    }
}
