using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ReadModelCompositionBoundaryTests {
    [Fact]
    public void ModuleProductionProjects_DoNotReferenceHostComposition() {
        IReadOnlyDictionary<string, string[]> references = ProjectReferenceReader.ReadProductionProjectReferences();
        Assert.All(references.Where(entry => entry.Key.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                || entry.Key.StartsWith("FoodDiary.Application.", StringComparison.Ordinal)),
            entry => Assert.DoesNotContain("FoodDiary.ReadModel.Composition", entry.Value, StringComparer.Ordinal));
    }

    [Fact]
    public void ModuleInfrastructure_DoesNotReferenceForeignDomainsOrComposition() {
        string root = ArchitectureTestPaths.FromRoot("Modules");
        foreach (string module in Directory.GetDirectories(root)) {
            string infrastructure = Path.Combine(module, "Infrastructure");
            if (!Directory.Exists(infrastructure)) { continue; }
            foreach (string project in Directory.GetFiles(infrastructure, "*.csproj")) {
                string owner = Path.GetFileName(module);
                string[] references = ProjectReferenceReader.ReadProjectReferences(
                    Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, project));
                Assert.DoesNotContain("FoodDiary.ReadModel.Composition", references, StringComparer.Ordinal);
                Assert.DoesNotContain(references, reference => reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                    && reference.EndsWith(".Domain", StringComparison.Ordinal)
                    && !string.Equals(reference, $"FoodDiary.Modules.{owner}.Domain", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void Composition_HasOnlyReadCapabilities() {
        string root = ArchitectureTestPaths.FromRoot("FoodDiary.ReadModel.Composition");
        IReadOnlyDictionary<string, string[]> capabilities = PersistenceCapabilityScanner.Scan(
            SourceScanner.SourceFiles(root).Select(path => (path, File.ReadAllText(path))));
        Assert.NotEmpty(capabilities);
        Assert.All(capabilities, entry => Assert.All(entry.Value, capability =>
            Assert.True(capability.StartsWith("entity:", StringComparison.Ordinal), $"{entry.Key}: {capability}")));
    }

    [Fact]
    public void Composition_DoesNotReferenceModuleImplementationsOrProviders() {
        string path = ArchitectureTestPaths.FromRoot("FoodDiary.ReadModel.Composition", "FoodDiary.ReadModel.Composition.csproj");
        var project = XDocument.Load(path);
        string[] references = ProjectReferenceReader.ReadProjectReferences(Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path));
        Assert.DoesNotContain(references, reference => reference.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
            && (reference.EndsWith(".Infrastructure", StringComparison.Ordinal) || reference.EndsWith(".Application", StringComparison.Ordinal)
                || reference.EndsWith(".Presentation", StringComparison.Ordinal)));
        Assert.Equal(["Microsoft.EntityFrameworkCore", "Microsoft.Extensions.DependencyInjection.Abstractions"],
            project.Descendants("PackageReference").Select(element => element.Attribute("Include")!.Value).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }
}
