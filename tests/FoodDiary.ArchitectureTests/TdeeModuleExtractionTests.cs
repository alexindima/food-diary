namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class TdeeModuleExtractionTests {
    [Fact]
    public void TdeeApplicationSource_LivesOnlyInLogicalModule() {
        string originalLegacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Tdee");
        string extractedLegacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Tdee");
        string moduleApplicationRoot = ArchitectureTestPaths.FromRoot("Modules", "Tdee", "Application");

        Assert.Empty(Directory.Exists(originalLegacyRoot) ? SourceScanner.SourceFiles(originalLegacyRoot) : []);
        Assert.Empty(Directory.Exists(extractedLegacyRoot) ? SourceScanner.SourceFiles(extractedLegacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(moduleApplicationRoot));
        Assert.True(File.Exists(Path.Combine(moduleApplicationRoot, "FoodDiary.Modules.Tdee.Application.csproj")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Tdee", "FoodDiary.Modules.Tdee.csproj")));
    }

    [Fact]
    public void TdeeLogicalModule_DoesNotCreateUnownedLayers() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Tdee", "Contracts")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Tdee", "Domain")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Tdee", "Infrastructure")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules", "Tdee", "Application", "Abstractions")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Users", "Domain", "Entities", "Users", "User.Tdee.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Abstractions",
            "Users",
            "Common",
            "IUserTdeeProfileReadService.cs")));
    }

    [Fact]
    public void TdeeApplicationAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Tdee/Application/FoodDiary.Modules.Tdee.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Domain", "FoodDiary.Mediator", "FoodDiary.Modules.Exercises.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Fact]
    public void TdeeApplicationAssembly_PreservesLegacyBinaryIdentity() {
        string project = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules",
            "Tdee",
            "Application",
            "FoodDiary.Modules.Tdee.Application.csproj"));

        Assert.Contains("<AssemblyName>FoodDiary.Application.Tdee</AssemblyName>", project, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterTdeeModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddTdeeModule()", source, StringComparison.Ordinal);
    }
}
