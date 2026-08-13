namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class IdentityModuleExtractionTests {
    [Theory]
    [InlineData("Authentication")]
    [InlineData("Email")]
    public void CoreApplicationProject_DoesNotContainIdentityImplementation(string feature) {
        Assert.False(Directory.Exists(ArchitectureTestPaths.FromRoot("FoodDiary.Application", feature)));
    }

    [Fact]
    public void IdentityProject_DoesNotReferenceCoreApplicationProject() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Application.Identity",
            "FoodDiary.Application.Identity.csproj"));

        Assert.DoesNotContain("..\\FoodDiary.Application\\FoodDiary.Application.csproj", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebApiCompositionRoot_RegistersIdentityModule() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot(
            "FoodDiary.Web.Api",
            "Extensions",
            "ApiServiceCollectionExtensions.cs"));

        Assert.Contains("AddIdentityModule()", source, StringComparison.Ordinal);
    }
}
