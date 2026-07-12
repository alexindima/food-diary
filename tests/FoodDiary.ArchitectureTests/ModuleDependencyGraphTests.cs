using System.Text.Json;
using System.Text.RegularExpressions;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed partial class ModuleDependencyGraphTests {
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
        string applicationRoot = Path.Combine(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application");
        string[] modules = [.. Directory.GetDirectories(applicationRoot)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => name is not ("bin" or "obj" or "Common"))
            .Where(name => Directory.EnumerateFiles(Path.Combine(applicationRoot, name), "*.cs", SearchOption.AllDirectories).Any())
            .Order(StringComparer.Ordinal)];
        HashSet<string> moduleSet = modules.ToHashSet(StringComparer.Ordinal);

        return modules.ToDictionary(
            static module => module,
            module => SourceScanner.SourceFiles(Path.Combine(applicationRoot, module))
                .SelectMany(File.ReadLines)
                .Select(line => ApplicationNamespaceRegex().Match(line))
                .Where(static match => match.Success)
                .Select(static match => match.Groups["module"].Value)
                .Where(dependency => moduleSet.Contains(dependency) && !dependency.Equals(module, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
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

    [GeneratedRegex(
        @"^using FoodDiary\.Application\.(?<module>[A-Za-z0-9_]+)(?:\.|;)",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1_000)]
    private static partial Regex ApplicationNamespaceRegex();

    [ExcludeFromCodeCoverage]
    private sealed record ModuleDependencyManifest(
        int SchemaVersion,
        Dictionary<string, string[]> Modules,
        string[][] KnownCycles);
}
