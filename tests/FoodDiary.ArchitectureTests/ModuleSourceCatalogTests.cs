namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleSourceCatalogTests {
    [Fact]
    public void ApplicationInventory_CoversEveryPhysicalModule() {
        string[] physicalModules = [.. Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))
            .Where(directory => Directory.Exists(Path.Combine(directory, "Application")))
            .Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];
        Assert.Equal(physicalModules, ModuleSourceCatalog.ApplicationRoots.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
        Assert.Contains(ModuleSourceCatalog.ApplicationFiles(), path => path.EndsWith(
            Path.Combine("Billing", "Application", "DependencyInjection.cs"), StringComparison.Ordinal));
        Assert.DoesNotContain(ModuleSourceCatalog.ApplicationFiles(), path => path.Contains(
            $"{Path.DirectorySeparatorChar}Abstractions{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRequiredRoot_FailsInsteadOfPassingWithEmptyInput() {
        string missing = Path.Combine(Path.GetTempPath(), $"fooddiary-missing-{Guid.NewGuid():N}");
        Assert.Throws<DirectoryNotFoundException>(() => ModuleSourceCatalog.RequiredFiles(missing));
    }

    [Fact]
    public void BillingScope_DetectsAForbiddenReferenceInCurrentSources() {
        string root = ModuleSourceCatalog.ApplicationRoot("Billing");
        Assert.NotEmpty(ModuleSourceCatalog.ApplicationFiles(root));
        Assert.NotEmpty(SourceScanner.FindLinePatternViolations(root, ["namespace FoodDiary.Application.Billing"]));
    }
}
