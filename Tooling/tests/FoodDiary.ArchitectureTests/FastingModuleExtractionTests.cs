namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class FastingModuleExtractionTests {
    [Fact]
    public void RuntimeRepositoriesUseOnlyOwnedSets() {
        string root = ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Infrastructure");
        foreach (string path in Directory.GetFiles(Path.Combine(root, "Persistence"), "*Repository.cs")) {
            string source = File.ReadAllText(path);
            Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
            Assert.Contains("DbSet<Fasting", source, StringComparison.Ordinal);
        }
        Assert.Contains("CreateModuleContext<FastingDbContext>", File.ReadAllText(Path.Combine(root, "ModuleRegistration.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void FastingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Fasting");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Fasting",
            "Application",
            "FoodDiary.Modules.Fasting.Application.csproj")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Fasting",
            "FoodDiary.Modules.Fasting.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Fasting",
            "Contracts",
            "FoodDiary.Modules.Fasting.Contracts.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Fasting",
            "Domain",
            "FoodDiary.Modules.Fasting.Domain.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Fasting",
            "Infrastructure",
            "FoodDiary.Modules.Fasting.Infrastructure.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedFastingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Modules.Fasting", references, StringComparer.Ordinal);
    }

    [Fact]
    public void FastingDomainSource_LivesOnlyInModuleDomainProject() {
        string centralRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Tracking", "Fasting");
        string moduleRoot = ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Domain");

        Assert.Empty(Directory.Exists(centralRoot) ? SourceScanner.SourceFiles(centralRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(moduleRoot));
    }

    [Fact]
    public void FastingPersistenceSource_LivesOnlyInModuleInfrastructureProjects() {
        string centralRepositoryRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Tracking");
        string moduleInfrastructureRoot = ArchitectureTestPaths.FromRoot("Modules", "Fasting", "Infrastructure");
        string[] centralFastingFiles = [.. SourceScanner.SourceFiles(centralRepositoryRoot)
            .Where(path => Path.GetFileName(path).StartsWith("Fasting", StringComparison.Ordinal))];

        Assert.Empty(centralFastingFiles);
        Assert.NotEmpty(SourceScanner.SourceFiles(moduleInfrastructureRoot));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterFastingModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddFastingModule()", source, StringComparison.Ordinal);
    }
}
