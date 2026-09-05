namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HydrationModuleExtractionTests {
    [Fact]
    public void HydrationRepository_ReceivesOnlyItsOwnedSet() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Hydration", "Infrastructure", "Persistence", "HydrationEntryRepository.cs"));
        Assert.Contains("HydrationEntryRepository(DbSet<HydrationEntry> entries)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Hydration/Domain/FoodDiary.Modules.Hydration.Domain.csproj");
        Assert.Equal(["FoodDiary.Domain.Primitives", "FoodDiary.Modules.Users.Domain.Contracts"], references, StringComparer.Ordinal);
    }

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
            "Modules/Hydration/Application/FoodDiary.Modules.Hydration.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Hydration.Application.Abstractions", "FoodDiary.Modules.Hydration.Contracts", "FoodDiary.Modules.Hydration.Domain", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void HydrationDomain_LivesOnlyInModuleProject_WithoutInverseUserNavigation() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Tracking", "HydrationEntry.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "HydrationEntryId.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Domain", "FoodDiary.Modules.Hydration.Domain.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Domain", "Entities", "Tracking", "HydrationEntry.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Hydration", "Domain", "ValueObjects", "Ids", "HydrationEntryId.cs")));

        string userSource = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "Users", "Domain", "Entities", "Users", "User.cs"));
        Assert.DoesNotContain("HydrationEntry", userSource, StringComparison.Ordinal);

        string configurationSource = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Hydration", "Infrastructure", "Model", "Configurations", "HydrationEntryConfiguration.cs"));
        Assert.Contains(".WithMany()", configurationSource, StringComparison.Ordinal);
        Assert.DoesNotContain("u => u.HydrationEntries", configurationSource, StringComparison.Ordinal);
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
