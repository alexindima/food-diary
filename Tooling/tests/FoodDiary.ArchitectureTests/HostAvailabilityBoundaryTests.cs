namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class HostAvailabilityBoundaryTests {
    [Fact]
    public void CoreApiStartup_RequiresPrimaryStorageWithoutMailInfrastructure() {
        string[] dependencies = ReadComposeDependencies("api");

        Assert.Multiple(
            () => Assert.Contains("db-init", dependencies, StringComparer.Ordinal),
            () => Assert.Contains("redis", dependencies, StringComparer.Ordinal),
            () => Assert.DoesNotContain("mail-relay", dependencies, StringComparer.Ordinal),
            () => Assert.DoesNotContain("rabbitmq", dependencies, StringComparer.Ordinal));
    }

    [Fact]
    public void MailRelayStartup_RequiresItsDatabaseWithoutBrokerAvailability() {
        string[] dependencies = ReadComposeDependencies("mail-relay");

        Assert.Multiple(
            () => Assert.Contains("mailrelay-db-init", dependencies, StringComparer.Ordinal),
            () => Assert.DoesNotContain("rabbitmq", dependencies, StringComparer.Ordinal));
    }

    private static string[] ReadComposeDependencies(string serviceName) {
        string[] lines = File.ReadAllLines(ArchitectureTestPaths.FromRoot("docker-compose.yml"));
        int start = Array.FindIndex(lines, line => line.Equals($"  {serviceName}:", StringComparison.Ordinal));
        Assert.True(start >= 0, $"Compose service '{serviceName}' was not found.");
        int end = Array.FindIndex(lines, start + 1, line =>
            line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("   ", StringComparison.Ordinal) &&
            line.EndsWith(':'));
        if (end < 0) {
            end = lines.Length;
        }
        int dependenciesStart = Array.FindIndex(lines, start + 1, end - start - 1,
            static line => line.Equals("    depends_on:", StringComparison.Ordinal));
        if (dependenciesStart < 0) {
            return [];
        }

        return [.. lines.Skip(dependenciesStart + 1).TakeWhile(static line => string.IsNullOrWhiteSpace(line) || line.StartsWith("     ", StringComparison.Ordinal))
            .Where(static line => line.StartsWith("      ", StringComparison.Ordinal) && !line.StartsWith("       ", StringComparison.Ordinal) && line.EndsWith(':'))
            .Select(static line => line.Trim()[..^1])];
    }
}
