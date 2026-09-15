namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingModuleExtractionTests {
    [Fact]
    public void MarketingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Marketing");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Marketing", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Modules.Marketing.Application.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedMarketingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Shared/FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Modules.Marketing.Application", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedMarketingAssembly_DoesNotReferenceCoreApplication() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Marketing/Application/FoodDiary.Modules.Marketing.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Application.Contracts", references, StringComparer.Ordinal);
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
    public void Billing_DependsOnMarketingContractsWithoutImplementationReference() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Billing/Application/FoodDiary.Modules.Billing.Application.csproj");
        Assert.Contains("FoodDiary.Modules.Marketing.Contracts", references, StringComparer.Ordinal);
        Assert.DoesNotContain("FoodDiary.Modules.Marketing.Application", references, StringComparer.Ordinal);
    }
}
