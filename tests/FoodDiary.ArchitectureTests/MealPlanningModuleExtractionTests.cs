namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MealPlanningModuleExtractionTests {
    [Theory]
    [InlineData("MealPlans")]
    [InlineData("ShoppingLists")]
    public void MealPlanningApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Application", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedMealPlanningAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.MealPlanning", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedMealPlanningAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/MealPlanning/Application/FoodDiary.Application.MealPlanning.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.MealPlanning.Application.Abstractions",
            "FoodDiary.Modules.MealPlanning.Domain",
        ];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterMealPlanningModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddMealPlanningModule()", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MealPlans", "MealPlan")]
    [InlineData("MealPlans", "MealPlanDay")]
    [InlineData("MealPlans", "MealPlanMeal")]
    [InlineData("ShoppingLists", "ShoppingList")]
    [InlineData("ShoppingLists", "ShoppingListItem")]
    [InlineData("ShoppingLists", "ShoppingListItemSource")]
    public void OwnedConfigurations_LiveOnlyInModulePersistenceModel(string area, string entity) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "MealPlanning", "Infrastructure", "Model", "Configurations", area, entity + "Configuration.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", "Configurations", area, entity + "Configuration.cs")));
    }

    [Theory]
    [InlineData("MealPlans", "MealPlanRepository.cs")]
    [InlineData("ShoppingLists", "ShoppingListRepository.cs")]
    public void OwnedRepositoriesAndPorts_LiveOnlyInModule(string area, string repository) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "MealPlanning", "Infrastructure", "Persistence", area, repository)));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Infrastructure", "Persistence", area, repository)));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", area)));
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot(
            "Modules", "MealPlanning", "Application", "Abstractions", area)));
    }

    [Theory]
    [InlineData("MealPlan.cs")]
    [InlineData("MealPlanDay.cs")]
    [InlineData("MealPlanMeal.cs")]
    public void MealPlanDomain_LivesOnlyInModule(string fileName) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "MealPlanning", "Domain", "Entities", "MealPlans", fileName)));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "MealPlans", fileName)));
    }

    [Fact]
    public void ShoppingListDomain_LivesOnlyInModuleWithoutReverseDomainReference() {
        string[] entityFiles = ["ShoppingList.cs", "ShoppingListItem.cs", "ShoppingListItemSource.cs"];
        string[] idFiles = ["MealPlanId.cs", "MealPlanMealId.cs", "ShoppingListId.cs", "ShoppingListItemId.cs", "ShoppingListItemSourceId.cs"];
        string[] eventFiles = ["ShoppingListItemAddedDomainEvent.cs", "ShoppingListItemsClearedDomainEvent.cs", "ShoppingListNameUpdatedDomainEvent.cs"];

        Assert.All(entityFiles, fileName => {
            Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Domain", "Entities", "Shopping", fileName)));
            Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Shopping", fileName)));
        });
        Assert.All(idFiles, fileName => {
            Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Domain", "ValueObjects", "Ids", fileName)));
            Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", fileName)));
        });
        Assert.All(eventFiles, fileName => {
            Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Domain", "Events", fileName)));
            Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Events", fileName)));
        });
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Domain", "Enums", "ShoppingListItemSourceType.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", "ShoppingListItemSourceType.cs")));
        Assert.DoesNotContain("FoodDiary.Modules.MealPlanning.Domain",
            ProjectReferenceReader.ReadProjectReferences("FoodDiary.Domain/FoodDiary.Domain.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void ShoppingListUserRelationship_IsOneWayAndExplicitlyMapped() {
        string userSource = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Users", "User.cs"));
        string mappingSource = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "MealPlanning", "Infrastructure", "Model", "Configurations", "ShoppingLists", "ShoppingListConfiguration.cs"));

        Assert.DoesNotContain("ShoppingLists", userSource, StringComparison.Ordinal);
        Assert.Contains(".WithMany()", mappingSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".WithMany(u => u.ShoppingLists)", mappingSource, StringComparison.Ordinal);
        Assert.Contains(".HasForeignKey(e => e.UserId)", mappingSource, StringComparison.Ordinal);
        Assert.Contains(".OnDelete(DeleteBehavior.Cascade)", mappingSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Application.Tests")]
    [InlineData("Domain.Tests")]
    [InlineData("Infrastructure.IntegrationTests")]
    public void FocusedTestProjects_LiveInModuleWithoutDonorProjectReferences(string suffix) {
        string projectName = "FoodDiary.Modules.MealPlanning." + suffix;
        string relativePath = $"Modules/MealPlanning/tests/{projectName}/{projectName}.csproj";
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(relativePath.Split('/'))));
        Assert.DoesNotContain(ProjectReferenceReader.ReadProjectReferences(relativePath),
            reference => reference.EndsWith(".Tests", StringComparison.Ordinal)
                || reference.EndsWith(".IntegrationTests", StringComparison.Ordinal));
    }

    [Fact]
    public void MealPlanToShoppingListCreation_RemainsBehindInternalAreaPort() {
        string portPath = ArchitectureTestPaths.FromRoot("Modules", "MealPlanning", "Application", "ShoppingLists",
            "Common", "IShoppingListCreationService.cs");
        Assert.True(File.Exists(portPath));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "ShoppingLists")));
    }

}
