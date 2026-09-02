namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class NutritionDomainExtractionTests {
    [Theory]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.FoodQualityScore), "ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.FoodQualityGrade), "ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.Enums.MeasurementUnit), "Enums")]
    public void NutritionTypes_HaveOnePhysicalOwner(Type type, string folder) {
        Assert.Equal("FoodDiary.Nutrition.Domain", type.Assembly.GetName().Name);
        Assert.Equal($"FoodDiary.Domain.{folder}", type.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Nutrition.Domain", folder, $"{type.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", folder, $"{type.Name}.cs")));
    }

    [Fact]
    public void NutritionOwner_IsLimitedToFoodScoringAndUnitsWithTemporaryGuardAccess() {
        string root = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Nutrition.Domain");
        string[] files = [.. SourceScanner.SourceFiles(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order(StringComparer.Ordinal)];
        Assert.Equal(["Enums/MeasurementUnit.cs", "ValueObjects/FoodQualityGrade.cs", "ValueObjects/FoodQualityScore.cs"], files, StringComparer.Ordinal);
        Assert.Equal(["FoodDiary.Domain", "FoodDiary.Modules.Products.Domain.Contracts"], ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Nutrition.Domain/FoodDiary.Nutrition.Domain.csproj"), StringComparer.Ordinal);
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Domain/FoodDiary.Domain.csproj"), StringComparer.Ordinal);
        Assert.Contains("InternalsVisibleTo(\"FoodDiary.Nutrition.Domain\")", File.ReadAllText(
            ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "AssemblyInfo.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void ProductContracts_ContainOnlyIdentityAndClassification() {
        Type type = typeof(FoodDiary.Domain.Enums.ProductType);
        Assert.Equal("FoodDiary.Modules.Products.Domain.Contracts", type.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Domain.Enums", type.Namespace);
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", "ProductType.cs")));
        string root = ArchitectureTestPaths.FromRoot("Modules", "Products", "Domain.Contracts");
        string[] files = [.. SourceScanner.SourceFiles(root).Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).Order(StringComparer.Ordinal)];
        Assert.Equal(["Enums/ProductType.cs", "ValueObjects/Ids/ProductId.cs"], files, StringComparer.Ordinal);
        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Products/Domain.Contracts/FoodDiary.Modules.Products.Domain.Contracts.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void FocusedScoringTests_MoveWithoutMovingMixedCentralCoverage() {
        string focusedTest = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "tests/FoodDiary.Nutrition.Domain.Tests/ValueObjects/FoodQualityScoreTests.cs"));
        string centralTest = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "tests/FoodDiary.Domain.Tests/Domain/ValueObjects/AdditionalValueObjectsInvariantTests.cs"));
        Assert.Contains("FoodQualityScore_Calculate_WithZeroCalories_ReturnsYellow50", focusedTest, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodQualityScore_Calculate_", centralTest, StringComparison.Ordinal);
        Assert.Contains("HealthAreaScores_Calculate_", centralTest, StringComparison.Ordinal);
    }
}
