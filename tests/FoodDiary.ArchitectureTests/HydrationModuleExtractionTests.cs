namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HydrationModuleExtractionTests {
    [Fact]
    public void HydrationApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Hydration");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedHydrationAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Hydration/FoodDiary.Modules.Hydration.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Hydration.Application.Abstractions",
            "FoodDiary.Modules.Hydration.Contracts",
        ], references);
    }

    [Fact]
    public void HydrationDomainCompatibilitySeam_RemainsCentral() {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Tracking", "HydrationEntry.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "HydrationEntryId.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Domain", "FoodDiary.Modules.Hydration.Domain.csproj")));

        string userSource = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Users", "User.cs"));
        Assert.Contains("IReadOnlyCollection<HydrationEntry> HydrationEntries", userSource, StringComparison.Ordinal);
    }

    [Fact]
    public void HydrationPersistence_LivesOnlyInModuleProjects() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Tracking", "HydrationEntryRepository.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "Hydration", "HydrationEntryConfiguration.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Infrastructure", "Persistence", "HydrationEntryRepository.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Infrastructure", "Model", "Configurations", "HydrationEntryConfiguration.cs")));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterHydrationModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddHydrationModule()", source, StringComparison.Ordinal);
    }
}
