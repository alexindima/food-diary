namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class WearablesModuleExtractionTests {
    [Fact]
    public void CoreApplicationProject_DoesNotContainWearablesImplementation() {
        string legacyRoot = ArchitectureTestPaths.FromRoot("FoodDiary.Application", "Wearables");

        Assert.False(Directory.Exists(legacyRoot));
    }

    [Fact]
    public void WearablesProject_DoesNotReferenceCoreApplicationProject() {
        string projectFile = ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Wearables",
            "FoodDiary.Application.Wearables.csproj");
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
