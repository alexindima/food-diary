namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WearablesModuleExtractionTests {
    [Fact]
    public void WearablesRunner_DelegatesToSessionCoordinator() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "Modules", "Wearables", "Infrastructure", "Persistence", "EfWearableTransactionRunner.cs"));
        Assert.Multiple(
            () => Assert.Contains("IModuleSessionCoordinator", source, StringComparison.Ordinal),
            () => Assert.Contains("coordinator.ExecuteSerializedAsync", source, StringComparison.Ordinal),
            () => Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal),
            () => Assert.DoesNotContain("IModuleTransactionCoordinator", source, StringComparison.Ordinal),
            () => Assert.DoesNotContain("SaveChangesAsync", source, StringComparison.Ordinal));
    }

    [Fact]
    public void CoreApplicationProject_DoesNotContainWearablesImplementation() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Wearables");

        Assert.False(Directory.Exists(legacyRoot));
    }

    [Fact]
    public void WearablesProject_DoesNotReferenceCoreApplicationProject() {
        string projectFile = ArchitectureTestPaths.FromRoot(
            "Modules", "Wearables", "Application",
            "FoodDiary.Modules.Wearables.Application.csproj");
        string references = File.ReadAllText(projectFile);

        Assert.DoesNotContain("..\\FoodDiary.Application\\FoodDiary.Application.csproj", references, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebApiCompositionRoot_RegistersWearablesModule() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Web.Api",
            "Extensions",
            "ApiServiceCollectionExtensions.cs"));

        Assert.Contains("AddWearablesModule()", source, StringComparison.Ordinal);
    }
}
