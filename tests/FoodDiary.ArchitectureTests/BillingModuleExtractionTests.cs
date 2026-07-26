namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BillingModuleExtractionTests {
    [Fact]
    public void BillingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Billing");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Billing");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Application.Billing.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedBillingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Billing", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedBillingAssembly_DoesNotReferenceCoreApplication() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Billing/FoodDiary.Application.Billing.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Application.Abstractions", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterBillingModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddBillingModule()", source, StringComparison.Ordinal);
    }
}
