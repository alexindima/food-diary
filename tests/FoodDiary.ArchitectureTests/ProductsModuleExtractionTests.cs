namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProductsModuleExtractionTests {
    [Theory]
    [InlineData("Application/FoodDiary.Modules.Products.Application.csproj")]
    [InlineData("Application/Abstractions/FoodDiary.Modules.Products.Application.Abstractions.csproj")]
    [InlineData("Contracts/FoodDiary.Modules.Products.Contracts.csproj")]
    [InlineData("Domain.Contracts/FoodDiary.Modules.Products.Domain.Contracts.csproj")]
    [InlineData("Domain/FoodDiary.Modules.Products.Domain.csproj")]
    [InlineData("Infrastructure/FoodDiary.Modules.Products.Infrastructure.csproj")]
    [InlineData("Infrastructure/Model/FoodDiary.Modules.Products.PersistenceModel.csproj")]
    [InlineData("tests/FoodDiary.Modules.Products.Application.Tests/FoodDiary.Modules.Products.Application.Tests.csproj")]
    [InlineData("tests/FoodDiary.Modules.Products.Domain.Tests/FoodDiary.Modules.Products.Domain.Tests.csproj")]
    [InlineData("tests/FoodDiary.Modules.Products.Infrastructure.IntegrationTests/FoodDiary.Modules.Products.Infrastructure.IntegrationTests.csproj")]
    public void OwnedLayer_HasPhysicalProject(string relativePath) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Products", relativePath)));
    }

    [Theory]
    [InlineData("FoodDiary.Application.Products")]
    [InlineData("FoodDiary.Application.Abstractions/Products")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Products")]
    [InlineData("FoodDiary.Infrastructure/Persistence/Configurations/Products")]
    public void DonorFolder_HasNoRemainingSources(string relativePath) {
        string path = ArchitectureTestPaths.FromRoot(relativePath);
        Assert.Empty(Directory.Exists(path) ? SourceScanner.SourceFiles(path) : []);
    }

    [Fact]
    public void ProductDomainOwnership_IsPhysicalAndAcyclic() {
        string user = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Users/Domain/Entities/Users/User.cs"));
        string product = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Products/Domain/Entities/Products/Product.cs"));
        string ingredient = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Recipes/Domain/Entities/Recipes/RecipeIngredient.cs"));
        string mealItem = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Meals/Domain/Entities/Meals/MealItem.cs"));
        Assert.DoesNotContain("IReadOnlyCollection<Product> Products", user, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyCollection<MealItem> MealItems", product, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipeIngredient", product, StringComparison.Ordinal);
        Assert.Contains("UsdaFood? UsdaFood", product, StringComparison.Ordinal);
        Assert.Contains("Product? Product", ingredient, StringComparison.Ordinal);
        Assert.DoesNotContain("Product? Product", mealItem, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyProductSnapshot(Product product)", mealItem, StringComparison.Ordinal);
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")));
    }

    [Fact]
    public void CentralDomainTests_DoNotOwnProductAggregateCoverage() {
        string tests = ArchitectureTestPaths.FromRoot("tests/FoodDiary.Domain.Tests");
        Assert.Empty(SourceScanner.FindLinePatternViolations(tests, [
            "Entities.Products",
            "ProductNutrition",
            "ProductIdentityState",
            "ProductIdentityUpdate",
            "ProductMeasurementState",
            "ProductMeasurementNutritionUpdate",
            "ProductMediaState",
        ]));
        // Mixed Products/USDA score invariants require the scoring owner, while aggregate tests stay module-owned.
        Assert.Contains("FoodDiary.Modules.Products.Domain.csproj", File.ReadAllText(
            ArchitectureTestPaths.FromRoot("tests/FoodDiary.Domain.Tests/FoodDiary.Domain.Tests.csproj")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ConsumedContracts_DoNotExposeAggregateRepositories() {
        string contracts = ArchitectureTestPaths.FromRoot("Modules/Products/Contracts");
        Assert.Empty(SourceScanner.FindLinePatternViolations(contracts, [
            "FoodDiary.Domain.Entities",
            "IProductRepository",
            "IProductReadRepository",
            "IProductWriteRepository",
        ]));
        string writePort = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules/Products/Application/Abstractions/Products/Common/IProductWriteRepository.cs"));
        Assert.DoesNotContain("IProductWriteRepository : IProductReadRepository", writePort, StringComparison.Ordinal);
    }

    [Fact]
    public void JobManager_ComposesPersistenceWithoutAddingProductHandlers() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.JobManager/Program.cs"));
        Assert.Contains("AddProductsPersistence()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddProductsModule()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddProductsApplication()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceModelAndTransactionBoundary_AreExplicit() {
        string context = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure/Persistence/FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyProductsPersistenceModel()", context, StringComparison.Ordinal);
        string infrastructure = ArchitectureTestPaths.FromRoot("Modules/Products/Infrastructure");
        string runner = Path.GetFullPath(Path.Combine(infrastructure, "Persistence", "Products", "EfProductMutationTransactionRunner.cs"));
        string runnerSource = File.ReadAllText(runner);
        Assert.Contains("RecipeCompositionTransactionLock.AcquireAsync", runnerSource, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync", runnerSource, StringComparison.Ordinal);
        Assert.DoesNotContain(SourceScanner.SourceFiles(infrastructure), path =>
            !string.Equals(Path.GetFullPath(path), runner, StringComparison.OrdinalIgnoreCase) &&
            File.ReadAllText(path).Contains("SaveChangesAsync(", StringComparison.Ordinal));
        Assert.DoesNotContain("FoodDiary.Modules.Products.Infrastructure", ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void ProductsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Products");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Products", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedProductsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Products/Application/FoodDiary.Modules.Products.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Application.Images", "FoodDiary.Application.Usda", "FoodDiary.Domain.Primitives", "FoodDiary.Mediator", "FoodDiary.Modules.Favorites.Contracts", "FoodDiary.Modules.OpenFoodFacts.Contracts", "FoodDiary.Modules.Products.Application.Abstractions", "FoodDiary.Modules.Products.Contracts", "FoodDiary.Modules.Products.Domain", "FoodDiary.Modules.Products.Domain.Contracts", "FoodDiary.Modules.RecentItems.Application.Abstractions", "FoodDiary.Modules.Usda.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterProductsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddProductsModule()", source, StringComparison.Ordinal);
    }
}
