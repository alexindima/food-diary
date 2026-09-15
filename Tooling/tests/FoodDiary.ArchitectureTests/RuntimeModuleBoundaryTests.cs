using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RuntimeModuleBoundaryTests {
    [Fact]
    public void ConsumerOwnedPorts_ExactlyMatchReviewedProvidersAndConsumers() {
        Source[] sources = [.. ModuleSourceCatalog.ApplicationRoots.Keys.SelectMany(owner =>
            SourceScanner.SourceFiles(ArchitectureTestPaths.FromRoot("Modules", owner))
                .Where(path => !path.Replace('\\', '/').Contains("/tests/", StringComparison.Ordinal))
                .Select(path => new Source(owner, Path.GetRelativePath(ArchitectureTestPaths.RepositoryRoot, path).Replace('\\', '/'),
                    CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot())))];
        var contracts = sources.SelectMany(source => source.Root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
            .Select(type => (Name: type.Identifier.ValueText, source.Owner)))
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Owner).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        Assert.All(contracts, contract => Assert.True(contract.Value.Length == 1,
            $"Ambiguous contract owner for {contract.Key}; resolve the namespace before recording runtime edges."));
        var consumersByContract = sources.Where(source => !source.Path.Contains("/Abstractions/", StringComparison.Ordinal))
            .SelectMany(source => source.Root.DescendantNodes().OfType<ParameterSyntax>()
                .SelectMany(parameter => parameter.Type?.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>() ?? [])
                .Select(name => (Contract: name.Identifier.ValueText, source.Owner)))
            .GroupBy(item => item.Contract, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Owner).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        string[] actual = [.. sources.SelectMany(source => source.Root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .SelectMany(type => type.BaseList?.Types.SelectMany(baseType => baseType.Type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                .Where(name => contracts.TryGetValue(name.Identifier.ValueText, out string[]? owners) && owners.Length == 1 && !string.Equals(owners[0], source.Owner, StringComparison.Ordinal))
                .Select(name => {
                    string contract = name.Identifier.ValueText;
                    string[] consumers = [.. consumersByContract.GetValueOrDefault(contract, []).Where(owner => !string.Equals(owner, source.Owner, StringComparison.Ordinal))
                        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
                    return string.Join('|', contract, contracts[contract][0], source.Owner, type.Identifier.ValueText, source.Path, string.Join(',', consumers));
                }) ?? []))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        using var manifest = JsonDocument.Parse(File.ReadAllText(ArchitectureTestPaths.FromRoot("docs", "architecture", "runtime-module-boundaries.json")));
        string[] expected = [.. manifest.RootElement.GetProperty("consumerOwnedPorts").EnumerateArray().Select(entry => {
            Assert.Contains(entry.GetProperty("transaction").GetString(), new[] { "read-only", "caller-unit-of-work", "owner-purge-transaction", "ambient-transaction-or-autocommit" }, StringComparer.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()));
            return string.Join('|', entry.GetProperty("contract").GetString(), entry.GetProperty("contractOwner").GetString(),
                entry.GetProperty("provider").GetString(), entry.GetProperty("implementation").GetString(), entry.GetProperty("source").GetString(),
                string.Join(',', entry.GetProperty("consumers").EnumerateArray().Select(value => value.GetString()).Order(StringComparer.Ordinal)));
        }).Order(StringComparer.Ordinal)];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DashboardItemComposition_UsesOwnerProjectionWithoutForeignTablesOrSnapshotPolicy() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("FoodDiary.ReadModel.Composition", "Dashboard", "DashboardMealItemsLoader.cs"));
        Assert.Contains("IMealItemDisplayReadService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Snapshot", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodQualityScore.Calculate", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UserConsumerProfiles_AreRegisteredToProjectionAdapter() {
        string registration = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "Users", "Infrastructure", "UsersModuleRegistration.cs"));
        Assert.Contains("AddScoped<UserProfileProjectionService>", registration, StringComparison.Ordinal);
        string application = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "Users", "Application", "DependencyInjection.cs"));
        Assert.DoesNotContain("AddScoped<ICurrentUserAccessService>", application, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<IUserHydrationProfileReadService>", application, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed record Source(string Owner, string Path, CompilationUnitSyntax Root);
}
