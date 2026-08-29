using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleDependencyGraphTests {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Manifest_ExactlyMatchesApplicationModuleDependencies() {
        ModuleDependencyManifest manifest = LoadManifest();
        IReadOnlyDictionary<string, string[]> actual = ReadActualGraph();

        Assert.Equal(manifest.Modules.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));
        foreach ((string module, string[] declaredDependencies) in manifest.Modules) {
            Assert.Equal(
                declaredDependencies.Order(StringComparer.Ordinal),
                actual[module].Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void Manifest_HasNoUnknownOrSelfDependencies() {
        ModuleDependencyManifest manifest = LoadManifest();
        string[] modules = [.. manifest.Modules.Keys];
        string[] violations = [.. manifest.Modules
            .SelectMany(pair => pair.Value.Select(dependency => (Module: pair.Key, Dependency: dependency)))
            .Where(edge => edge.Module.Equals(edge.Dependency, StringComparison.Ordinal) ||
                           !modules.Contains(edge.Dependency, StringComparer.Ordinal))
            .Select(edge => $"{edge.Module} -> {edge.Dependency}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void Manifest_DoesNotIntroduceUnacknowledgedCycles() {
        ModuleDependencyManifest manifest = LoadManifest();
        string[] actualCycles = [.. FindStronglyConnectedComponents(manifest.Modules)
            .Where(component => component.Length > 1)
            .Select(NormalizeCycle)
            .Order(StringComparer.Ordinal)];
        string[] acknowledgedCycles = [.. manifest.KnownCycles
            .Select(NormalizeCycle)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(acknowledgedCycles, actualCycles);
    }

    private static IReadOnlyDictionary<string, string[]> ReadActualGraph() {
        var declaredModules = LoadManifest().Modules.Keys.ToHashSet(StringComparer.Ordinal);
        var moduleRoots = Directory.GetDirectories(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application.*", SearchOption.TopDirectoryOnly)
            .SelectMany(directory => Directory.GetFiles(directory, "FoodDiary.Application.*.csproj", SearchOption.TopDirectoryOnly))
            .Select(path => new {
                Name = Path.GetFileNameWithoutExtension(path)["FoodDiary.Application.".Length..],
                Root = Path.GetDirectoryName(path)!,
            })
            .Concat(Directory.Exists(ArchitectureTestPaths.FromRoot("Modules"))
                ? Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"), "*", SearchOption.TopDirectoryOnly)
                    .Where(directory => Directory.GetFiles(directory, "FoodDiary.Modules.*.csproj", SearchOption.TopDirectoryOnly).Length == 1)
                    .Select(directory => new { Name = Path.GetFileName(directory), Root = directory })
                : [])
            .Where(module => declaredModules.Contains(module.Name))
            .OrderBy(module => module.Name, StringComparer.Ordinal)
            .ToArray();
        var moduleSet = moduleRoots.Select(module => module.Name).ToHashSet(StringComparer.Ordinal);

        return moduleRoots.ToDictionary(
            static module => module.Name,
            module => SourceScanner.SourceFiles(module.Root)
                .SelectMany(ReadReferencedApplicationModules)
                .Where(dependency => moduleSet.Contains(dependency) && !dependency.Equals(module.Name, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
    }

    private static IEnumerable<string> ReadReferencedApplicationModules(string path) {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
        IEnumerable<string?> names = root.Usings
            .Select(usingDirective => usingDirective.Name?.ToString())
            .Concat(root.DescendantNodes()
                .OfType<NameSyntax>()
                .Select(name => name.ToString()));

        foreach (string name in names.OfType<string>()) {
            string prefix = name.StartsWith("FoodDiary.Modules.", StringComparison.Ordinal)
                ? "FoodDiary.Modules."
                : "FoodDiary.Application.";
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                name.StartsWith("FoodDiary.Application.Abstractions.", StringComparison.Ordinal)) {
                continue;
            }

            string remainder = name[prefix.Length..];
            int separator = remainder.IndexOf('.', StringComparison.Ordinal);
            yield return separator < 0 ? remainder : remainder[..separator];
        }
    }

    private static ModuleDependencyManifest LoadManifest() {
        string path = ArchitectureTestPaths.FromRoot("docs", "architecture", "module-dependencies.json");
        return JsonSerializer.Deserialize<ModuleDependencyManifest>(
                   File.ReadAllText(path),
                   SerializerOptions) ??
               throw new InvalidOperationException("Module dependency manifest is empty.");
    }

    private static IEnumerable<string[]> FindStronglyConnectedComponents(IReadOnlyDictionary<string, string[]> graph) {
        int index = 0;
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowLinks = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<string[]>();

        foreach (string node in graph.Keys.Order(StringComparer.Ordinal)) {
            if (!indices.ContainsKey(node)) {
                Visit(node);
            }
        }

        return components;

        void Visit(string node) {
            indices[node] = index;
            lowLinks[node] = index++;
            stack.Push(node);
            onStack.Add(node);

            foreach (string dependency in graph[node]) {
                if (!indices.ContainsKey(dependency)) {
                    Visit(dependency);
                    lowLinks[node] = Math.Min(lowLinks[node], lowLinks[dependency]);
                } else if (onStack.Contains(dependency)) {
                    lowLinks[node] = Math.Min(lowLinks[node], indices[dependency]);
                }
            }

            if (lowLinks[node] != indices[node]) {
                return;
            }

            var component = new List<string>();
            string current;
            do {
                current = stack.Pop();
                onStack.Remove(current);
                component.Add(current);
            } while (!current.Equals(node, StringComparison.Ordinal));

            components.Add([.. component.Order(StringComparer.Ordinal)]);
        }
    }

    private static string NormalizeCycle(IEnumerable<string> modules) =>
        string.Join(" <-> ", modules.Order(StringComparer.Ordinal));

    [ExcludeFromCodeCoverage]
    private sealed record ModuleDependencyManifest(
        int SchemaVersion,
        Dictionary<string, string[]> Modules,
        string[][] KnownCycles);
}
