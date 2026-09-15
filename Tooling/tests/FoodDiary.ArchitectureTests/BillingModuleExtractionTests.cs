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
            "Shared/FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj");

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
    [Fact]
    public void BillingTransactions_UseScopedCoordinatorAndKeepOwnerErrorTranslation() {
        string runner = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Billing/Infrastructure/Persistence/EfBillingTransactionRunner.cs"));
        string registration = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules/Billing/Infrastructure/ModuleRegistration.cs"));
        foreach (string source in new[] { runner, registration }) {
            Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChangesAsync(", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BeginTransactionAsync(", source, StringComparison.Ordinal);
        }
        Assert.Contains("coordinator.ExecuteAsync(", runner, StringComparison.Ordinal);
        Assert.Contains("coordinator.CurrentTransaction", registration, StringComparison.Ordinal);
        Assert.Contains("IX_BillingPayments_Provider_ExternalPaymentId", runner, StringComparison.Ordinal);
        Assert.Contains("IX_BillingWebhookEvents_Provider_EventId", runner, StringComparison.Ordinal);
    }
}
