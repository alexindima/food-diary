namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BillingModuleExtractionTests {
    [Fact]
    public void BillingPersistenceModel_RemainsInInfrastructureSourceCoverage() {
        Assert.Contains(ArchitectureTestPaths.FromRoot("Modules", "Billing", "PersistenceModel"),
            ModuleSourceCatalog.InfrastructureRoots(), StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Application", "Services/BillingOverviewReadService.cs")]
    [InlineData("Application", "Common/IBillingOverviewReadService.cs")]
    [InlineData("Application", "Services/BillingUserContextService.cs")]
    [InlineData("Application", "Common/IBillingUserContextService.cs")]
    [InlineData("Application", "Models/BillingUserProfileModel.cs")]
    [InlineData("Application.Abstractions", "Common/IBillingSubscriptionReadRepository.cs")]
    [InlineData("Application.Abstractions", "Common/IBillingSubscriptionRepository.cs")]
    [InlineData("Application.Abstractions", "Common/IBillingPaymentRepository.cs")]
    [InlineData("Application.Abstractions", "Common/IBillingWebhookEventRepository.cs")]
    public void RetiredForwardingLayers_DoNotReturn(string project, string file) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot("Modules", "Billing", project, file)));
    }

    [Fact]
    public void BillingApplicationSource_LivesOnlyInExtractedAssembly() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Billing");
        string extractedRoot = ArchitectureTestPaths.FromRoot("Modules", "Billing", "Application");

        Assert.Empty(Directory.Exists(legacyRoot) ? SourceScanner.SourceFiles(legacyRoot) : []);
        Assert.NotEmpty(SourceScanner.SourceFiles(extractedRoot));
        Assert.True(File.Exists(Path.Combine(extractedRoot, "FoodDiary.Modules.Billing.Application.csproj")));
    }

    [Fact]
    public void CoreApplication_DoesNotReferenceExtractedBillingAssembly() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

        Assert.DoesNotContain("FoodDiary.Modules.Billing.Application", references, StringComparer.Ordinal);
    }

    [Fact]
    public void ExtractedBillingAssembly_DoesNotReferenceCoreApplication() {
        string[] references = ProjectReferenceReader.ReadProjectReferences(
            "Modules/Billing/Application/FoodDiary.Modules.Billing.Application.csproj");

        Assert.DoesNotContain("FoodDiary.Application", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Application.Contracts", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Billing.Application.Abstractions", references, StringComparer.Ordinal);
        Assert.Contains("FoodDiary.Modules.Billing.Domain", references, StringComparer.Ordinal);
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
