namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class UsersModuleExtractionTests {
    [Fact]
    public void UsersApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Users");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Users");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Application.Users.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedUsersAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Users", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedUsersAssembly_DoesNotReferenceCoreApplication() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Users/FoodDiary.Application.Users.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Application.Abstractions", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterUsersModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddUsersModule()", source, StringComparison.Ordinal);
    }
}
