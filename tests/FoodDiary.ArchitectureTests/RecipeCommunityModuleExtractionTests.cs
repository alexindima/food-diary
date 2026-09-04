namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCommunityModuleExtractionTests {
    [Theory]
    [InlineData("RecipeComments")]
    [InlineData("RecipeLikes")]
    public void RecipeCommunityApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "RecipeCommunity", "Application", feature);

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
            "Modules/RecipeCommunity/Application/FoodDiary.Application.RecipeCommunity.csproj");
        string[] expectedReferences = ["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.Notifications.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Application.Abstractions", "FoodDiary.Modules.RecipeCommunity.Domain", "FoodDiary.Modules.Recipes.Application.Abstractions", "FoodDiary.Modules.Recipes.Contracts", "FoodDiary.Modules.Users.Contracts", "FoodDiary.Modules.Users.Domain.Contracts"];

        Assert.Equal(expectedReferences, references);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterRecipeCommunityModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddRecipeCommunityModule()", source, StringComparison.Ordinal);
    }
    [Theory]
    [InlineData("Domain/Entities/Recipes/RecipeComment.cs", "FoodDiary.Domain/Entities/Recipes/RecipeComment.cs")]
    [InlineData("Domain/Entities/Social/RecipeLike.cs", "FoodDiary.Domain/Entities/Social/RecipeLike.cs")]
    [InlineData("Domain/ValueObjects/Ids/RecipeCommentId.cs", "FoodDiary.Domain/ValueObjects/Ids/RecipeCommentId.cs")]
    [InlineData("Domain/ValueObjects/Ids/RecipeLikeId.cs", "FoodDiary.Domain/ValueObjects/Ids/RecipeLikeId.cs")]
    [InlineData("Infrastructure/Persistence/RecipeComments/RecipeCommentRepository.cs", "FoodDiary.Infrastructure/Persistence/RecipeComments/RecipeCommentRepository.cs")]
    [InlineData("Infrastructure/Persistence/RecipeLikes/RecipeLikeRepository.cs", "FoodDiary.Infrastructure/Persistence/RecipeLikes/RecipeLikeRepository.cs")]
    public void OwnedSource_HasOneModuleLocation(string modulePath, string donorPath) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "RecipeCommunity", modulePath)));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(donorPath)));
    }

    [Fact]
    public void CentralDomain_DoesNotReferenceRecipeCommunityDomain() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain/FoodDiary.Domain.csproj")));
    }

    [Fact]
    public void SharedContext_ExplicitlyAppliesRecipeCommunityModel() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure", "Persistence", "FoodDiaryDbContext.cs"));
        Assert.Contains("ApplyRecipeCommunityPersistenceModel()", source, StringComparison.Ordinal);
    }
}
