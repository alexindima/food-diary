using System.Text.Json;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class ModuleSourceCatalog {
    public static IReadOnlyDictionary<string, string> ApplicationRoots { get; } = ReadApplicationRoots();

    public static string ApplicationRoot(string feature) {
        string owner = feature switch {
            "Authentication" or "Email" => "Identity",
            "WeightEntries" or "WaistEntries" => "BodyMetrics",
            "FavoriteProducts" or "FavoriteRecipes" or "FavoriteMeals" => "Favorites",
            "MealPlans" or "ShoppingLists" => "MealPlanning",
            "RecipeComments" or "RecipeLikes" => "RecipeCommunity",
            _ => feature,
        };
        return ApplicationRoots.TryGetValue(owner, out string? root)
            ? root
            : throw new InvalidOperationException($"Unknown application owner: {feature}.");
    }

    public static IEnumerable<string> ApplicationFiles() =>
        ApplicationRoots.Values.SelectMany(ApplicationFiles);

    public static IEnumerable<string> ApplicationFiles(string root) =>
        RequiredFiles(root).Where(path => !Path.GetRelativePath(root, path)
            .StartsWith($"Abstractions{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    public static IEnumerable<string> InfrastructureRoots() =>
        ApplicationRoots.Values.Select(root => Path.Combine(Path.GetDirectoryName(root)!, "Infrastructure"))
            .Where(Directory.Exists)
            .Prepend(ArchitectureTestPaths.FromRoot("FoodDiary.Infrastructure"));

    public static IEnumerable<string> InfrastructureFiles() =>
        InfrastructureRoots().SelectMany(RequiredFiles)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    public static string[] RequiredFiles(string root) {
        if (!Directory.Exists(root)) {
            throw new DirectoryNotFoundException($"Required architecture source root does not exist: {root}");
        }
        string[] files = [.. SourceScanner.SourceFiles(root)];
        if (files.Length == 0) {
            throw new InvalidOperationException($"Required architecture source root contains no C# sources: {root}");
        }
        return files;
    }

    private static Dictionary<string, string> ReadApplicationRoots() {
        using var manifest = JsonDocument.Parse(File.ReadAllText(
            ArchitectureTestPaths.FromRoot("docs", "architecture", "backend-modules.json")));
        return manifest.RootElement.GetProperty("modules").EnumerateObject().ToDictionary(
            module => module.Name,
            module => {
                string relativePath = module.Value.GetProperty("sourceMappings")
                    .GetProperty("applicationProjects").EnumerateArray().Single().GetString()!;
                string root = ArchitectureTestPaths.FromRoot(relativePath.Replace('/', Path.DirectorySeparatorChar));
                _ = RequiredFiles(root);
                return root;
            },
            StringComparer.Ordinal);
    }
}
