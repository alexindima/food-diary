using System.Xml.Linq;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ApplicationConsumerBoundaryTests {
    [Fact]
    public void Modules_NeverReferenceAnotherModulesApplicationImplementation() {
        string modules = ArchitectureTestPaths.FromRoot("Modules");
        string[] applicationProjects = [.. Directory.GetDirectories(modules)
            .SelectMany(module => Directory.GetFiles(Path.Combine(module, "Application"), "*.csproj"))];
        Dictionary<string, string> owners = applicationProjects.ToDictionary(Path.GetFullPath,
            project => Directory.GetParent(project)!.Parent!.Name, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();
        foreach (string project in applicationProjects) {
            string owner = owners[Path.GetFullPath(project)];
            foreach (System.Xml.Linq.XElement reference in XDocument.Load(project).Descendants("ProjectReference")) {
                string target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, reference.Attribute("Include")!.Value));
                if (owners.TryGetValue(target, out string? targetOwner) && !string.Equals(owner, targetOwner, StringComparison.Ordinal)) {
                    violations.Add($"{owner} -> {targetOwner}");
                }
            }
        }
        Assert.Empty(violations);
    }

    [Fact]
    public void EveryOutboxStream_FencesEfWritesByClaimOwner() {
        using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=architecture;Username=test;Password=test").Options);
        Microsoft.EntityFrameworkCore.Metadata.IEntityType[] streams = [.. context.Model.GetEntityTypes()
            .Where(entity => typeof(IOutboxMessage).IsAssignableFrom(entity.ClrType))];
        Assert.Equal(4, streams.Length);
        Assert.All(streams, entity => Assert.True(entity.FindProperty(nameof(IOutboxMessage.LockedBy))!.IsConcurrencyToken));
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
