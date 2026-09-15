namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleExtractionTests {
    [Fact]
    public void RepositoryPorts_ExposeOnlyWritesAndReadModels() {
        string folder = ArchitectureTestPaths.FromRoot("Modules/Cycles/Application.Abstractions/Common");
        string[] ports = [.. Directory.GetFiles(folder, "I*Repository.cs").Select(static path => Path.GetFileName(path)!).Order(StringComparer.Ordinal)];
        Assert.Equal(["ICycleReadModelRepository.cs", "ICycleWriteRepository.cs"], ports);
        Assert.False(File.Exists(Path.Combine(folder, "CycleDayErrors.cs")));
    }

    [Fact]
    public void RuntimeRepositoryReceivesOnlyTheOwnedAggregateSet() {
        string registration = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Cycles/Infrastructure/ModuleRegistration.cs"));
        string repository = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Cycles/Infrastructure/Persistence/CycleRepository.cs"));
        Assert.Contains("CreateModuleContext<CyclesDbContext>", registration, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<CyclesDbContext>().CycleProfiles", registration, StringComparison.Ordinal);
        Assert.Contains("DbSet<CycleProfile> profiles", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiaryDbContext", repository, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.BleedingType))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.CycleSymptomCategory))]
    [InlineData(typeof(FoodDiary.Modules.Cycles.Domain.Contracts.Enums.OvulationTestResult))]
    public void CycleEnums_AreOwnedOnlyByCyclesDomainContracts(Type enumType) {
        Assert.Equal("FoodDiary.Modules.Cycles.Domain.Contracts", enumType.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Modules.Cycles.Domain.Contracts.Enums", enumType.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Domain.Contracts", "Enums", $"{enumType.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", $"{enumType.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")));
    }

    [Fact]
    public void CyclesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Cycles");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void RetiredCycleId_HasNoRuntimeType() {
        Assert.Null(typeof(FoodDiary.Modules.Cycles.Domain.Entities.CycleProfile).Assembly.GetType(
            "FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids.CycleId"));
    }

    [Fact]
    public void ExtractedCyclesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Cycles/Application/FoodDiary.Modules.Cycles.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Contracts", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Cycles.Domain.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts", "FoodDiary.Results"], references);
    }

    [Fact]
    public void CyclesPersistenceOwnership_IsModuleOwnedWhileDbContextAndMigrationsStayCentral() {
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "PersistenceModel", "Configurations")));
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Infrastructure", "Persistence")));
        Assert.Empty(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "Cycles"))
            ? SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "Configurations", "Cycles")) : []);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs")));
        Assert.NotEmpty(Directory.GetFiles(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Migrations"), "*Cycle*.cs"));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterCyclesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddCyclesModule()", source, StringComparison.Ordinal);
    }
}
