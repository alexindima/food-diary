using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class PersistenceCapabilityTests {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Fact]
    public void ModuleAdapters_UseOnlyReviewedPersistenceCapabilities() {
        (string Path, string Source)[] sources = [.. ModuleSourceCatalog.InfrastructureFiles()
            .Where(path => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path).StartsWith($"Modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Model{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path: Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path).Replace('\\', '/'), Source: File.ReadAllText(path)))];
        IReadOnlyDictionary<string, string[]> actual = PersistenceCapabilityScanner.Scan(
            sources.Select(source => (source.Path, WithProjectUsings(source.Path, source.Source))));
        string manifestPath = ArchitectureTestPaths.FromRoot("docs", "architecture", "persistence-capabilities.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var expected = manifest.RootElement.GetProperty("adapters").EnumerateObject().ToDictionary(
            entry => entry.Name,
            entry => entry.Value.GetProperty("capabilities").EnumerateArray().Select(value => value.GetString()!).Order(StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);
        string[] violations = [.. actual.Keys.Union(expected.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Where(path => !actual.GetValueOrDefault(path, []).SequenceEqual(expected.GetValueOrDefault(path, []), StringComparer.Ordinal))
            .Select(path => $"{path}: actual [{string.Join(", ", actual.GetValueOrDefault(path, []))}], reviewed [{string.Join(", ", expected.GetValueOrDefault(path, []))}]")];
        if (violations.Length > 0) {
            string artifactDirectory = ArchitectureTestPaths.FromRoot(".artifacts", "architecture-improvements");
            Directory.CreateDirectory(artifactDirectory);
            File.WriteAllText(Path.Combine(artifactDirectory, "persistence-access.actual.json"), JsonSerializer.Serialize(actual, JsonOptions));
        }
        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
        Assert.All(manifest.RootElement.GetProperty("adapters").EnumerateObject(), adapter =>
            Assert.False(string.IsNullOrWhiteSpace(adapter.Value.GetProperty("reason").GetString()), $"{adapter.Name}: missing review reason"));
        foreach ((string path, string[] capabilities) in actual) {
            Assert.DoesNotContain(capabilities, capability => capability.StartsWith("unresolved:", StringComparison.Ordinal));
            if (!capabilities.Any(capability => capability.StartsWith("context:", StringComparison.Ordinal))) { continue; }
            string source = sources.Single(candidate => candidate.Path.Equals(path, StringComparison.Ordinal)).Source;
            string normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
            string fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
            string reviewed = manifest.RootElement.GetProperty("adapters").GetProperty(path).GetProperty("technicalSourceSha256").GetString()!;
            Assert.True(fingerprint.Equals(reviewed, StringComparison.Ordinal),
                $"{path}: review SQL, ChangeTracker and context escape paths after this change; technicalSourceSha256={fingerprint}");
        }
    }

    private static string WithProjectUsings(string relativePath, string source) {
        string[] segments = relativePath.Split('/');
        string projectDirectory = ArchitectureTestPaths.FromRoot(segments[0], segments[1], "Infrastructure");
        string project = Directory.GetFiles(projectDirectory, "*.csproj").Single();
        string[] imports = [.. XDocument.Load(project).Descendants("Using").Select(element => {
            Assert.Null(element.Attribute("Alias"));
            Assert.Null(element.Attribute("Static"));
            return $"using {element.Attribute("Include")!.Value};";
        })];
        return string.Join(Environment.NewLine, imports) + Environment.NewLine + source;
    }

    [Theory]
    [InlineData("await db.Users.AsNoTracking().ExecuteDeleteAsync();", "write:User")]
    [InlineData("var users = db.Users.AsNoTracking(); await users.ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));", "write:User")]
    [InlineData("db.Users.Add(User.Create(\"user@example.com\", \"hash\"));", "write:User")]
    [InlineData("var users = db.Set<User>();", "tracked:User")]
    [InlineData("await db.SaveChangesAsync();", "context:SaveChangesAsync")]
    [InlineData("await db.Users.Where(u => db.Users.AsNoTracking().Any()).ToListAsync();", "tracked:User")]
    public void Scanner_DetectsCapabilityEscalationIncludingQueryAliases(string body, string expected) {
        string source = "using FoodDiary.Infrastructure.Persistence; using FoodDiary.Domain.Entities.Users; using Microsoft.EntityFrameworkCore; using System.Threading.Tasks; class Probe(FoodDiaryDbContext db) { public async Task RunAsync() { " + body + " } }";
        IReadOnlyDictionary<string, string[]> actual = PersistenceCapabilityScanner.Scan([("probe.cs", source)]);
        Assert.Contains(expected, actual["probe.cs"], StringComparer.Ordinal);
    }

    [Fact]
    public void NoTrackingRead_DoesNotReceiveWriteOrTrackedCapabilities() {
        const string source = "using FoodDiary.Infrastructure.Persistence; using Microsoft.EntityFrameworkCore; class Probe(FoodDiaryDbContext db) { public object Read() => db.Users.AsNoTracking(); }";
        IReadOnlyDictionary<string, string[]> actual = PersistenceCapabilityScanner.Scan([("probe.cs", source)]);
        Assert.Equal(["entity:User"], actual["probe.cs"], StringComparer.Ordinal);
    }
}
