namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ProviderAdapterOwnershipTests {
    [Theory]
    [InlineData("FoodDiary.Integrations/Services/OpenAi/OpenAiFoodClient.cs", "Modules/Ai/Infrastructure/Providers/Services/OpenAi/OpenAiFoodClient.cs", "FoodDiary.Integrations.Services.OpenAi")]
    [InlineData("FoodDiary.Integrations/Services/OpenAi/OpenAiRequestFactory.cs", "Modules/Ai/Infrastructure/Providers/Services/OpenAi/OpenAiRequestFactory.cs", "FoodDiary.Integrations.Services.OpenAi")]
    [InlineData("FoodDiary.Integrations/Services/OpenAi/OpenAiErrorMetadata.cs", "Modules/Ai/Infrastructure/Providers/Services/OpenAi/OpenAiErrorMetadata.cs", "FoodDiary.Integrations.Services.OpenAi")]
    [InlineData("FoodDiary.Integrations/Options/OpenAiOptions.cs", "Modules/Ai/Infrastructure/Providers/Options/OpenAiOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Services/OpenFoodFactsService.cs", "Modules/OpenFoodFacts/Infrastructure/Providers/Services/OpenFoodFactsService.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Options/OpenFoodFactsApiOptions.cs", "Modules/OpenFoodFacts/Infrastructure/Providers/Options/OpenFoodFactsApiOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Services/UsdaFoodSearchService.cs", "Modules/Usda/Infrastructure/Providers/Services/UsdaFoodSearchService.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/UsdaFoodDetailCache.cs", "Modules/Usda/Infrastructure/Providers/Services/UsdaFoodDetailCache.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/UsdaFoodDetailLookupResult.cs", "Modules/Usda/Infrastructure/Providers/Services/UsdaFoodDetailLookupResult.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Options/UsdaApiOptions.cs", "Modules/Usda/Infrastructure/Providers/Options/UsdaApiOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Wearables/FitbitClient.cs", "Modules/Wearables/Infrastructure/Providers/Wearables/FitbitClient.cs", "FoodDiary.Integrations.Wearables")]
    [InlineData("FoodDiary.Integrations/Options/FitbitOptions.cs", "Modules/Wearables/Infrastructure/Providers/Options/FitbitOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Authentication/GoogleTokenValidator.cs", "Modules/Identity/Infrastructure/Providers/Authentication/GoogleTokenValidator.cs", "FoodDiary.Integrations.Authentication")]
    [InlineData("FoodDiary.Integrations/Authentication/TelegramAuthTimestampValidator.cs", "Modules/Identity/Infrastructure/Providers/Authentication/TelegramAuthTimestampValidator.cs", "FoodDiary.Integrations.Authentication")]
    [InlineData("FoodDiary.Integrations/Authentication/TelegramAuthValidator.cs", "Modules/Identity/Infrastructure/Providers/Authentication/TelegramAuthValidator.cs", "FoodDiary.Integrations.Authentication")]
    [InlineData("FoodDiary.Integrations/Authentication/TelegramLoginWidgetValidator.cs", "Modules/Identity/Infrastructure/Providers/Authentication/TelegramLoginWidgetValidator.cs", "FoodDiary.Integrations.Authentication")]
    [InlineData("FoodDiary.Integrations/Options/GoogleAuthOptions.cs", "Modules/Identity/Infrastructure/Providers/Options/GoogleAuthOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Options/TelegramAuthOptions.cs", "Modules/Identity/Infrastructure/Providers/Options/TelegramAuthOptions.cs", "FoodDiary.Integrations.Options")]
    [InlineData("FoodDiary.Integrations/Services/S3ImageStorageService.cs", "Modules/Images/Infrastructure/Providers/Services/S3ImageStorageService.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/S3ObjectStorageClient.cs", "Modules/Images/Infrastructure/Providers/Services/S3ObjectStorageClient.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/IObjectStorageClient.cs", "Modules/Images/Infrastructure/Providers/Services/IObjectStorageClient.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/StoredObjectInfo.cs", "Modules/Images/Infrastructure/Providers/Services/StoredObjectInfo.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Services/UnconfiguredImageStorageService.cs", "Modules/Images/Infrastructure/Providers/Services/UnconfiguredImageStorageService.cs", "FoodDiary.Integrations.Services")]
    [InlineData("FoodDiary.Integrations/Options/S3Options.cs", "Modules/Images/Infrastructure/Providers/Options/S3Options.cs", "FoodDiary.Integrations.Options")]
    public void ProviderSources_HaveOnePhysicalOwnerAndPreserveClrNamespace(string oldPath, string ownedPath, string expectedNamespace) {
        Assert.False(File.Exists(ArchitectureTestPaths.FromRoot(oldPath)));
        string path = ArchitectureTestPaths.FromRoot(ownedPath);
        Assert.True(File.Exists(path), ownedPath);
        Assert.NotEmpty(CSharpSyntaxReader.ReadTypeDeclarations(path));
        Assert.Contains($"namespace {expectedNamespace};", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ai")]
    [InlineData("Wearables")]
    [InlineData("Usda")]
    [InlineData("OpenFoodFacts")]
    [InlineData("Images")]
    public void ProviderOwner_DependsOnSharedIntegrationHelpersWithoutReverseExports(string module) {
        string[] references = ProjectReferenceReader.ReadProjectReferences($"Modules/{module}/Infrastructure/FoodDiary.Modules.{module}.Infrastructure.csproj");
        Assert.Contains("FoodDiary.Integrations", references, StringComparer.Ordinal);
        string[] central = ProjectReferenceReader.ReadProjectReferences("FoodDiary.Integrations/FoodDiary.Integrations.csproj");
        Assert.DoesNotContain(central, reference => reference.StartsWith($"FoodDiary.Modules.{module}.", StringComparison.Ordinal));
        string centralComposition = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Integrations/DependencyInjection.cs"));
        Assert.DoesNotContain($"Add{module}Provider", centralComposition, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs")]
    [InlineData("FoodDiary.JobManager/Program.cs")]
    public void ExistingProviderHosts_ExplicitlyComposeAllOwners(string path) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(path));
        foreach (string module in new[] { "Ai", "Wearables", "Usda", "OpenFoodFacts", "Identity", "Images" }) {
            Assert.Contains($".Add{module}Provider(", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Initializer_DoesNotAcquireExternalProviderConfiguration() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.Initializer/Program.cs"));
        foreach (string module in new[] { "Ai", "Wearables", "Usda", "OpenFoodFacts", "Identity", "Images" }) {
            Assert.DoesNotContain($"Add{module}Provider(", source, StringComparison.Ordinal);
        }
    }
    [Fact]
    public void IdentityProviders_DoNotNeedSharedIntegrationHelpersOrReverseExports() {
        string[] owner = ProjectReferenceReader.ReadProjectReferences("Modules/Identity/Infrastructure/FoodDiary.Modules.Identity.Infrastructure.csproj");
        string[] central = ProjectReferenceReader.ReadProjectReferences("FoodDiary.Integrations/FoodDiary.Integrations.csproj");
        Assert.DoesNotContain("FoodDiary.Integrations", owner, StringComparer.Ordinal);
        Assert.DoesNotContain(central, reference => reference.StartsWith("FoodDiary.Modules.Identity.", StringComparison.Ordinal));
        Assert.DoesNotContain(central, reference => reference.StartsWith("FoodDiary.Modules.Images.", StringComparison.Ordinal));
    }
}
