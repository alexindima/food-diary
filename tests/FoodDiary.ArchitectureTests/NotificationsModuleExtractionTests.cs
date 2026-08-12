namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class NotificationsModuleExtractionTests {
    [Fact]
    public void NotificationsApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Notifications");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Notifications");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Application.Notifications.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedNotificationsAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Notifications", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedNotificationsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Notifications/FoodDiary.Application.Notifications.csproj");
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
    public void ExecutableCompositionRoots_RegisterNotificationsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddNotificationsModule()", source, StringComparison.Ordinal);
    }
}
