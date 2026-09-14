using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleContextFactoryBoundaryTests {
    [Theory]
    [InlineData("DailyAdvices")]
    [InlineData("ContentReports")]
    [InlineData("Favorites")]
    [InlineData("Exercises")]
    [InlineData("RecipeCommunity")]
    [InlineData("Fasting")]
    [InlineData("Usda")]
    [InlineData("Marketing")]
    [InlineData("Lessons")]
    [InlineData("WeeklyGoals")]
    [InlineData("Wearables")]
    [InlineData("OpenFoodFacts")]
    public void CoordinatedAdapter_DoesNotDependOnCentralInfrastructureTransitively(string module) {
        IReadOnlyDictionary<string, string[]> graph = ProjectReferenceReader.ReadProductionProjectReferences();
        string project = $"FoodDiary.Modules.{module}.Infrastructure";
        Assert.Contains("FoodDiary.Persistence.Abstractions", graph[project], StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        pending.Enqueue(project);
        while (pending.TryDequeue(out string? current)) {
            if (!visited.Add(current)) {
                continue;
            }

            foreach (string dependency in graph[current]) {
                Assert.False(string.Equals(dependency, "FoodDiary.Infrastructure", StringComparison.Ordinal),
                    $"{project} depends on central Infrastructure through {current} -> {dependency}");
                pending.Enqueue(dependency);
            }
        }
    }

    [Theory]
    [InlineData("Identity", "IdentityModuleRegistration.cs")]
    [InlineData("Users", "UsersModuleRegistration.cs")]
    [InlineData("OpenFoodFacts", "ModuleRegistration.cs")]
    public void CoordinatedRegistration_DoesNotResolveConcreteSharedContext(string module, string fileName) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", module, "Infrastructure", fileName));
        Assert.Contains("GetRequiredService<IModuleTransactionCoordinator>()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FoodDiaryDbContext", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ai", "ModuleRegistration.cs")]
    [InlineData("Gamification", "ModuleRegistration.cs")]
    [InlineData("Identity", "IdentityModuleRegistration.cs")]
    [InlineData("Users", "UsersModuleRegistration.cs")]
    [InlineData("OpenFoodFacts", "ModuleRegistration.cs")]
    public void Registration_DoesNotReadTransactionsThroughDatabaseFacade(string module, string fileName) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", module, "Infrastructure", fileName));
        IEnumerable<MemberAccessExpressionSyntax> accesses = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<MemberAccessExpressionSyntax>();
        Assert.Contains(accesses, access => access.Name.Identifier.ValueText.Equals("CurrentTransaction", StringComparison.Ordinal));
        Assert.DoesNotContain(accesses, access => access.Name.Identifier.ValueText.Equals("CurrentTransaction", StringComparison.Ordinal)
            && access.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Database" });
    }

    [Fact]
    public void EveryModuleContext_IsCreatedThroughFactoryContract() {
        string modules = ArchitectureTestPaths.FromRoot("Modules");
        string[] files = [.. SourceScanner.SourceFiles(modules)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(".CreateModuleContext<", StringComparison.Ordinal))];
        Assert.Equal(29, files.Length);
        foreach (string path in files) {
            string source = File.ReadAllText(path);
            Assert.Contains("GetRequiredService<IModuleContextFactory>()", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService<FoodDiaryDbContext>()\n            .CreateModuleContext", source.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
            InvocationExpressionSyntax creation = Assert.Single(CSharpSyntaxTree.ParseText(source).GetRoot()
                .DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CreateModuleContext" });
            MemberAccessExpressionSyntax access = Assert.IsType<MemberAccessExpressionSyntax>(creation.Expression);
            Assert.True(access.Expression.ToString().Contains("GetRequiredService<IModuleContextFactory>()", StringComparison.Ordinal)
                || access.Expression is IdentifierNameSyntax { Identifier.ValueText: "factory" }, path);
        }
    }
}
