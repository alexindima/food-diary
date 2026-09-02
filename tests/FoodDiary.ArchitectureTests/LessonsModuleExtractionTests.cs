namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class LessonsModuleExtractionTests {
    [Fact]
    public void LessonsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Lessons");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Lessons", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedLessonsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Lessons/Application/FoodDiary.Modules.Lessons.Application.csproj");
        Assert.Equal(["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.Gamification.Application.Abstractions", "FoodDiary.Modules.Lessons.Application.Abstractions", "FoodDiary.Modules.Lessons.Contracts", "FoodDiary.Modules.Lessons.Domain", "FoodDiary.Modules.Users.Domain.Contracts"], references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterLessonsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddLessonsModule()", source, StringComparison.Ordinal);
    }
}
