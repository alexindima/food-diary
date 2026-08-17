namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCommunityModuleExtractionTests {
    [Theory]
    [InlineData("RecipeComments")]
    [InlineData("RecipeLikes")]
    public void RecipeCommunityApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.RecipeCommunity", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedRecipeCommunityAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.RecipeCommunity", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedRecipeCommunityAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.RecipeCommunity/FoodDiary.Application.RecipeCommunity.csproj");
        string[] expectedReferences = [
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
        ];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterRecipeCommunityModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddRecipeCommunityModule()", source, StringComparison.Ordinal);
    }
}
