namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class AiModuleExtractionTests {
    [Fact]
    public void PersistenceModel_DoesNotDependOnApplicationPorts() {
        string root = ArchitectureTestPaths.FromRoot("Modules/Ai/PersistenceModel");
        Assert.DoesNotContain("FoodDiary.Modules.Ai.Application.Abstractions",
            ProjectReferenceReader.ReadProjectReferences("Modules/Ai/PersistenceModel/FoodDiary.Modules.Ai.PersistenceModel.csproj"), StringComparer.Ordinal);
        Assert.Empty(SourceScanner.FindLinePatternViolations(root, ["Application.Abstractions", "AiQuotaReservationRequest"], requireSourceRoot: true));
    }

    [Fact]
    public void PromptPorts_ExposeOnlyUsedReadModelsAndOwnerWrites() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Ai", "Application.Abstractions", "Common");
        Assert.False(File.Exists(Path.Combine(root, "IAiUsageWriteRepository.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "Persistence", "AiUsageRepository.cs")));
        Assert.False(File.Exists(Path.Combine(root, "IAiPromptTemplateRepository.cs")));
        Assert.False(File.Exists(Path.Combine(root, "IAiPromptTemplateReadRepository.cs")));
        Assert.True(File.Exists(Path.Combine(root, "IAiPromptTemplateReadModelRepository.cs")));
        Assert.True(File.Exists(Path.Combine(root, "IAiPromptTemplateWriteRepository.cs")));
    }

    [Fact]
    public void Infrastructure_ReferencesTheOwnersOfConsumedEntitiesAndImageIdsDirectly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences("Modules/Ai/Infrastructure/FoodDiary.Modules.Ai.Infrastructure.csproj");
        Assert.Contains("FoodDiary.Modules.Ai.Domain", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Images.Contracts", references, StringComparer.Ordinal);
    }

    [Fact]
    public void Admin_ConsumesSemanticAiCapabilitiesWithoutQuotaOrPromptRepositories() {
        string adminRoot = ArchitectureTestPaths.FromRoot("Modules", "Admin", "Application");
        Assert.NotEmpty(SourceScanner.SourceFiles(adminRoot));
        Assert.Empty(SourceScanner.FindLinePatternViolations(adminRoot, [
            "IAiQuotaRepository", "IAiPromptTemplateRepository", "IAiPromptTemplateWriteRepository",
            "IAiPromptTemplateReadRepository", "IAiUsageQuery", "IAiUsageWriteRepository",
        ]));
    }

    [Theory]
    [InlineData("Contracts", "FoodDiary.Modules.Ai.Contracts.csproj")]
    [InlineData("Application", "FoodDiary.Modules.Ai.Application.csproj")]
    [InlineData("Application.Abstractions", "FoodDiary.Modules.Ai.Application.Abstractions.csproj")]
    [InlineData("Domain", "FoodDiary.Modules.Ai.Domain.csproj")]
    [InlineData("Infrastructure", "FoodDiary.Modules.Ai.Infrastructure.csproj")]
    [InlineData("PersistenceModel", "FoodDiary.Modules.Ai.PersistenceModel.csproj")]
    public void OwnedLayers_HavePhysicalProjects(string folder, string project) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Ai", folder, project)));
    }

    [Fact]
    public void DomainExtraction_PreservesOneWayIdentityAndMealsOwnership() {
        Assert.Equal(["FoodDiary.Modules.Users.Domain.Contracts"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Ai/Domain/FoodDiary.Modules.Ai.Domain.csproj"));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Meals", "Domain", "Entities", "MealAiSession.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Meals", "Domain", "Entities", "MealAiItem.cs")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Ai")));
    }

    [Fact]
    public void PersistenceModel_IsExplicitWithoutCentralAdapterDependency() {
        string[] references = ProjectReferenceReader.ReadProjectReferences("FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj");
        Assert.Contains("FoodDiary.Modules.Ai.PersistenceModel", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Ai.Infrastructure", references, StringComparer.Ordinal);
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyAiPersistenceModel()", source, StringComparison.Ordinal);
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Ai")));
    }

    [Fact]
    public void AiApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Ai");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Ai", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedAiAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Ai/Application/FoodDiary.Modules.Ai.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Ai.Application.Abstractions", "FoodDiary.Modules.Ai.Contracts", "FoodDiary.Modules.Ai.Domain", "FoodDiary.Modules.Images.Contracts", "FoodDiary.Modules.Images.Service.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterAiModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddAiModule()", source, StringComparison.Ordinal);
    }
}
