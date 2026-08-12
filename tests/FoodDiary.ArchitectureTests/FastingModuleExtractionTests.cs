namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class FastingModuleExtractionTests {
    [Fact]
    public void FastingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Fasting");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Fasting");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Fasting",
            "FoodDiary.Application.Fasting.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedFastingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Fasting", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedFastingAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Fasting/FoodDiary.Application.Fasting.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ];

        Assert.Equal(expectedReferences, references);
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
