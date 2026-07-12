namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingModuleExtractionTests {
    [Fact]
    public void MarketingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Marketing");
        string extractedRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.Marketing");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Application.Marketing.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedMarketingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application/FoodDiary.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application.Marketing", references, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterMarketingModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddMarketingModule()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Billing_DependsOnConsumerOwnedMarketingPort() {
        string billingRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Billing");
        string[] violations = [.. SourceScanner.SourceFiles(billingRoot)
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(entry => entry.line.Contains("FoodDiary.Application.Marketing", StringComparison.Ordinal) ||
                            entry.line.Contains("IMarketingConversionRecorder", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")];

        Assert.Empty(violations);
    }
}
