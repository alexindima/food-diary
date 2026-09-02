namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class EventGovernanceTests {
    public static IEnumerable<object[]> DomainModules() =>
        Directory.GetDirectories(ArchitectureTestPaths.FromRoot("Modules"))
            .Where(path => Directory.Exists(Path.Combine(path, "Domain", "Events")))
            .Select(path => new object[] { Path.GetFileName(path) });

    [Theory]
    [MemberData(nameof(DomainModules))]
    public void DomainEvents_AreRaisedByDomainModel(string module) {
        string domainRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain");
        string eventsRoot = Path.Combine(domainRoot, "Events");
        string[] domainSource = [.. SourceScanner.SourceFiles(domainRoot)
            .Where(path => !path.StartsWith(eventsRoot, StringComparison.OrdinalIgnoreCase))
            .SelectMany(File.ReadLines)];

        string[] orphanEvents = [.. SourceScanner.SourceFiles(eventsRoot)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(eventName => !domainSource.Any(line => line.Contains($"new {eventName}(", StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(orphanEvents);
    }

    [Theory]
    [MemberData(nameof(DomainModules))]
    public void DomainEvents_StayTransportAndProviderAgnostic(string module) {
        string eventsRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain", "Events");
        string[] forbiddenNamespaces = [
            "FoodDiary.Application",
            "FoodDiary.Infrastructure",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "System.Net.Http",
            "System.Text.Json",
        ];

        string[] violations = [.. SourceScanner.SourceFiles(eventsRoot)
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(entry => forbiddenNamespaces.Any(entry.line.Contains))
            .Select(entry => $"{Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(DomainModules))]
    public void DomainEventDeclarations_FollowImmutableNamingContract(string module) {
        string eventsRoot = ArchitectureTestPaths.FromRoot("Modules", module, "Domain", "Events");
        string[] violations = [.. SourceScanner.SourceFiles(eventsRoot)
            .Select(path => new { path, source = File.ReadAllText(path), name = Path.GetFileNameWithoutExtension(path) })
            .Where(entry => !entry.name.EndsWith("DomainEvent", StringComparison.Ordinal) ||
                            !entry.source.Contains($"public sealed record {entry.name} : IDomainEvent", StringComparison.Ordinal) ||
                            !entry.source.Contains("DateTime OccurredOnUtc", StringComparison.Ordinal))
            .Select(entry => Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, entry.path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void IntegrationEventImplementations_UseExplicitNameAndEventsFolder() {
        Assert.Empty(FindIntegrationEventViolations(ArchitectureTestPaths.RepositoryRoot));
    }

    internal static string[] FindIntegrationEventViolations(string root) =>
        [.. SourceScanner.SourceFiles(root)
            .Where(path => !path.StartsWith(Path.Combine(root, "tests"), StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => new { path, line, index }))
            .Where(entry => entry.line.Contains(": IIntegrationEvent", StringComparison.Ordinal))
            .Where(entry => !entry.path.Contains($"{Path.DirectorySeparatorChar}Events{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                            !Path.GetFileNameWithoutExtension(entry.path).EndsWith("IntegrationEvent", StringComparison.Ordinal))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{(entry.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .Order(StringComparer.Ordinal)];
}
