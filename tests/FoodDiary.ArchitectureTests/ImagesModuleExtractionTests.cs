namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ImagesModuleExtractionTests {
    [Fact]
    public void ImagesApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Images");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Images", "Application");
        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
    }

    [Fact]
    public void ExtractedImagesAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Images/Application/FoodDiary.Application.Images.csproj");
        Assert.Equal([
            "FoodDiary.Application.Abstractions",
            "FoodDiary.Domain",
            "FoodDiary.Mediator",
            "FoodDiary.Modules.Images.Application.Abstractions",
            "FoodDiary.Modules.Images.Domain",
        ], references);
    }

    [Fact]
    public void ImagesDomainOwnership_IsPhysicalAndAcyclic() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Assets", "ImageAsset.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "ImageAssetId.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Images", "Domain", "Entities", "Assets", "ImageAsset.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Images", "Contracts", "ValueObjects", "Ids", "ImageAssetId.cs")));

        Assert.Equal(["FoodDiary.Domain.Primitives"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Images/Contracts/FoodDiary.Modules.Images.Contracts.csproj"));
        Assert.Equal(["FoodDiary.Domain", "FoodDiary.Modules.Images.Contracts"], ProjectReferenceReader.ReadProjectReferences(
            "Modules/Images/Domain/FoodDiary.Modules.Images.Domain.csproj"));
    }

    [Fact]
    public void MealAiSession_HasNoImagesDomainNavigation() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Meals", "Domain", "Entities", "Meals", "MealAiSession.cs"));

        Assert.DoesNotContain("ImageAsset? ImageAsset", source, StringComparison.Ordinal);
        Assert.Contains("ImageAssetId? ImageAssetId", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterImagesModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));
        Assert.Contains("AddImagesModule()", source, StringComparison.Ordinal);
    }
}
