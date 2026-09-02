namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BodyMetricsModuleExtractionTests {
    [Theory]
    [InlineData("WeightEntries")]
    [InlineData("WaistEntries")]
    public void BodyMetricsApplicationSource_LivesOnlyInExtractedAssembly(string feature) {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application.BodyMetrics", feature);
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Application", feature);

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules",
            "BodyMetrics",
            "Application",
            "FoodDiary.Application.BodyMetrics.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedBodyMetricsAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Application.BodyMetrics", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedBodyMetricsAssembly_HasOnlyApprovedProjectReferences() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/BodyMetrics/Application/FoodDiary.Application.BodyMetrics.csproj");
        string[] expectedReferences = ["FoodDiary.Application.Abstractions", "FoodDiary.Mediator", "FoodDiary.Modules.BodyMetrics.Application.Abstractions", "FoodDiary.Modules.BodyMetrics.Domain", "FoodDiary.Modules.Users.Domain", "FoodDiary.Modules.Users.Domain.Contracts"];

        Assert.Equal(expectedReferences, references);
    }

    [Fact]
    public void BodyMetricsDomain_LivesOnlyInModuleProject_WithoutInverseUserNavigations() {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Tracking", "WeightEntry.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "Entities", "Tracking", "WaistEntry.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "WeightEntryId.cs")));
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Domain", "ValueObjects", "Ids", "WaistEntryId.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Domain", "FoodDiary.Modules.BodyMetrics.Domain.csproj")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Domain", "Entities", "Tracking", "WeightEntry.cs")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "BodyMetrics", "Domain", "Entities", "Tracking", "WaistEntry.cs")));

        string userSource = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "Users", "Domain", "Entities", "Users", "User.cs"));
        Assert.DoesNotContain("WeightEntry", userSource, StringComparison.Ordinal);
        Assert.DoesNotContain("WaistEntry", userSource, StringComparison.Ordinal);

        foreach ((string feature, string navigation) in new[] { ("WeightEntry", "WeightEntries"), ("WaistEntry", "WaistEntries") }) {
            string configurationSource = File.ReadAllText(ArchitectureTestPaths.FromRoot(
                "Modules", "BodyMetrics", "Infrastructure", "Model", "Configurations", $"{feature}Configuration.cs"));
            Assert.Contains(".WithMany()", configurationSource, StringComparison.Ordinal);
            Assert.DoesNotContain($"u => u.{navigation}", configurationSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BodyMetricsOwnedContracts_LiveInModuleAbstractions() {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "WeightEntries")));
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application.Abstractions", "WaistEntries")));
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "BodyMetrics", "Application", "Abstractions", "FoodDiary.Modules.BodyMetrics.Application.Abstractions.csproj")));
    }

    [Theory]
    [InlineData("WeightEntryConfiguration.cs")]
    [InlineData("WaistEntryConfiguration.cs")]
    public void BodyMetricsEntryConfigurations_LiveInModulePersistenceModel(string fileName) {
        Assert.True(File.Exists(ArchitectureTestPaths.FromRoot(
            "Modules", "BodyMetrics", "Infrastructure", "Model", "Configurations", fileName)));
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.Initializer/Program.cs")]
    public void ExecutableCompositionRoots_RegisterBodyMetricsModule(string relativePath) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(relativePath.Split('/')));

        Assert.Contains("AddBodyMetricsModule()", source, StringComparison.Ordinal);
    }
}
