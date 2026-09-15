namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class FavoritesModuleExtractionTests {
    [Theory]
    [InlineData("FavoriteMeals")]
    [InlineData("FavoriteProducts")]
    [InlineData("FavoriteRecipes")]
    public void FavoritesApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Favorites", "Application", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedFavoritesAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Modules.Favorites.Application", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterFavoritesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddFavoritesModule()", source, StringComparison.Ordinal);
    }
}
