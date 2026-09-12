namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RetiredDomainAssemblyTests {
    [Theory]
    [InlineData(typeof(FoodDiary.Domain.Enums.RoleNames), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/Enums", "FoodDiary.Domain.Enums")]
    [InlineData(typeof(FoodDiary.Domain.Enums.MeasurementUnit), "FoodDiary.Modules.Products.Domain.Contracts", "Modules/Products/Domain.Contracts/Enums", "FoodDiary.Domain.Enums")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.FoodQualityScore), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.FoodQualityGrade), "FoodDiary.Modules.Products.FoodQuality", "Modules/Products/FoodQuality/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.HealthAreaScore), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.HealthAreaGrade), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.HealthAreaScores), "FoodDiary.Modules.Usda.Domain", "Modules/Usda/Domain/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.DesiredWeightKg), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.DesiredWaistCm), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.LanguageCode), "FoodDiary.Modules.Users.Domain.Contracts", "Modules/Users/Domain.Contracts/ValueObjects", "FoodDiary.Domain.ValueObjects")]
    [InlineData(typeof(FoodDiary.Domain.Primitives.EmailAddress), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
    [InlineData(typeof(FoodDiary.Domain.Primitives.Visibility), "FoodDiary.Domain.Primitives", "Shared/FoodDiary.Domain.Primitives", "FoodDiary.Domain.Primitives")]
    [InlineData(typeof(FoodDiary.Domain.ValueObjects.Ids.CycleId), "FoodDiary.Modules.Cycles.Domain", "Modules/Cycles/Domain/ValueObjects/Ids", "FoodDiary.Domain.ValueObjects.Ids")]
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
