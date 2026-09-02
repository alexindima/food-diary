namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleExtractionTests {
    [Theory]
    [InlineData(typeof(FoodDiary.Domain.Enums.BleedingType))]
    [InlineData(typeof(FoodDiary.Domain.Enums.CycleSymptomCategory))]
    [InlineData(typeof(FoodDiary.Domain.Enums.OvulationTestResult))]
    public void CycleEnums_AreOwnedOnlyByCyclesDomain(Type enumType) {
        Assert.Equal("FoodDiary.Modules.Cycles.Domain", enumType.Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Domain.Enums", enumType.Namespace);
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Domain", "Enums", $"{enumType.Name}.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Enums", $"{enumType.Name}.cs")));
        Assert.DoesNotContain("FoodDiary.Modules.Cycles.Domain", ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Domain/FoodDiary.Domain.csproj"), StringComparer.Ordinal);
    }

    [Fact]
    public void CyclesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Cycles");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedCyclesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Cycles/Application/FoodDiary.Application.Cycles.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Domain", "FoodDiary.Mediator", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Domain", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void CyclesPersistenceOwnership_IsModuleOwnedWhileDbContextAndMigrationsStayCentral() {
        Assert.NotEmpty(SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", "Cycles", "Infrastructure", "Model", "Configurations")));
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
