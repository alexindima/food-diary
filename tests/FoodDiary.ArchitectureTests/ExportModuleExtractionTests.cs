namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ExportModuleExtractionTests {
    [Fact]
    public void ExportApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Export");
        string legacyAbstractionsRoot = ArchitectureTestPaths.FromRoot("Shared", "FoodDiary.Application.Contracts", "Export");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Export", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.Empty(Directory.Exists(legacyAbstractionsRoot) ? SourceScanner.SourceFiles(legacyAbstractionsRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedExportAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Authentication.Contracts", "FoodDiary.Mediator", "FoodDiary.Modules.Cycles.Application.Abstractions", "FoodDiary.Modules.Cycles.Contracts", "FoodDiary.Modules.Cycles.Domain.Contracts", "FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Modules.Identity.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void ExportAbstractions_HaveOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Export/Application/Abstractions/FoodDiary.Modules.Export.Application.Abstractions.csproj");
        Assert.Equal(["FoodDiary.Application.Contracts", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterExportModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddExportModule()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportPdfAdapter_IsOwnedByModuleWithoutCentralInfrastructureDependency() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Export/Infrastructure/FoodDiary.Modules.Export.Infrastructure.csproj");
        Assert.Equal(["FoodDiary.Modules.Export.Application.Abstractions", "FoodDiary.Modules.Meals.Contracts", "FoodDiary.Modules.Meals.Domain", "FoodDiary.Modules.Meals.Domain.Contracts"], references);
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Services", "DiaryPdf");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "DependencyInjection.Export.cs")));
        Assert.NotEmpty(SourceScanner.SourceFiles(
            ArchitectureTestPaths.FromRoot("Modules", "Export", "Infrastructure", "Services", "DiaryPdf")));
        Assert.DoesNotContain("QuestPDF", ProjectReferenceReader.ReadPackageReferences(
            "FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj"), StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    public void ExecutableCompositionRoots_RegisterExportAdapterExplicitly(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddExportInfrastructure()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportPdfTests_LiveWithTheirAdapter() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(
            "tests", "FoodDiary.Infrastructure.Tests", "Services", "DiaryPdfGeneratorTests.cs")));
        string testsRoot = ArchitectureTestPaths.FromRoot(
            "Modules", "Export", "tests", "FoodDiary.Modules.Export.Infrastructure.Tests");
        Assert.True(File.Exists(Path.Combine(testsRoot, "Services", "DiaryPdfGeneratorTests.cs")));
        Assert.True(File.Exists(Path.Combine(testsRoot, "ModuleRegistrationTests.cs")));
    }
}
