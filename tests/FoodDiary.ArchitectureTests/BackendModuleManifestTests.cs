using System.Text.Json;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class BackendModuleManifestTests {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void BoundaryManifest_CoversFolderAndExtractedApplicationModules() {
        BackendModuleManifest manifest = LoadManifest();
        string[] folderModules = [.. Directory.GetDirectories(ArchitectureTestPaths.FromRoot("FoodDiary.Application"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(name => name is not ("bin" or "obj" or "Common"))
            .Where(name => Directory.EnumerateFiles(
                ArchitectureTestPaths.FromRoot("FoodDiary.Application", name),
                "*.cs",
                SearchOption.AllDirectories).Any())
            .Order(StringComparer.Ordinal)];
        string[] extractedModules = [.. Directory.GetDirectories(ArchitectureTestPaths.RepositoryRoot, "FoodDiary.Application.*", SearchOption.TopDirectoryOnly)
            .SelectMany(directory => Directory.GetFiles(directory, "FoodDiary.Application.*.csproj", SearchOption.TopDirectoryOnly))
            .Select(path => Path.GetFileNameWithoutExtension(path)["FoodDiary.Application.".Length..])
            .Where(name => !name.Equals("Abstractions", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
        string[] actualModules = [.. folderModules.Concat(extractedModules).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

        Assert.Equal(manifest.Inventory.FolderModules, folderModules.Length);
        Assert.Equal(manifest.Inventory.ExtractedModules, extractedModules.Length);
        Assert.Equal(manifest.Inventory.TotalModules, actualModules.Length);
        Assert.Equal(manifest.Modules.Keys.Order(StringComparer.Ordinal), actualModules, StringComparer.Ordinal);
    }

    [Fact]
    public void BoundaryManifest_HasUniqueDeclaredEntityOwnersAndValidMappings() {
        BackendModuleManifest manifest = LoadManifest();
        string[] duplicateOwners = [.. manifest.Modules
            .SelectMany(pair => pair.Value.OwnedEntities.Select(entity => (Entity: entity, Module: pair.Key)))
            .GroupBy(pair => pair.Entity, StringComparer.Ordinal)
            .Where(group => group.Select(pair => pair.Module).Distinct(StringComparer.Ordinal).Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(pair => pair.Module).Order(StringComparer.Ordinal))}")
            .Order(StringComparer.Ordinal)];
        string[] invalidEntries = [.. manifest.Modules
            .Where(pair => string.IsNullOrWhiteSpace(pair.Value.Role) ||
                           string.IsNullOrWhiteSpace(pair.Value.PhysicalIsolation) ||
                           string.IsNullOrWhiteSpace(pair.Value.Enforcement))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(duplicateOwners);
        Assert.Empty(invalidEntries);
    }

    private static BackendModuleManifest LoadManifest() {
        string path = ArchitectureTestPaths.FromRoot("docs", "architecture", "backend-modules.json");
        return JsonSerializer.Deserialize<BackendModuleManifest>(File.ReadAllText(path), SerializerOptions) ??
               throw new InvalidOperationException("Backend module boundary manifest is empty.");
    }

    [ExcludeFromCodeCoverage]
    private sealed record BackendModuleManifest(
        int SchemaVersion,
        ModuleInventory Inventory,
        Dictionary<string, ModuleBoundary> Modules);

    [ExcludeFromCodeCoverage]
    private sealed record ModuleInventory(int FolderModules, int ExtractedModules, int TotalModules);

    [ExcludeFromCodeCoverage]
    private sealed record ModuleBoundary(
        string Role,
        string PhysicalIsolation,
        string Enforcement,
        string[] OwnedEntities,
        Dictionary<string, JsonElement> SourceMappings);
}
