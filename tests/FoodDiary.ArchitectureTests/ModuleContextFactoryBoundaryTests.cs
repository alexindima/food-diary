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
    [InlineData("Billing")]
    [InlineData("Hydration")]
    [InlineData("BodyMetrics")]
    [InlineData("Cycles")]
    [InlineData("OpenFoodFacts")]
    [InlineData("Admin")]
    [InlineData("Meals")]
    [InlineData("MealPlanning")]
    [InlineData("RecentItems")]
    [InlineData("Products")]
    [InlineData("Recipes")]
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
    [InlineData("Ai", "ModuleRegistration.cs")]
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
    public void EveryModuleRegistration_UsesFactoryContractsWithoutConcreteSharedContext() {
        string modules = ArchitectureTestPaths.FromRoot("Modules");
        string[] files = [.. SourceScanner.SourceFiles(modules)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Infrastructure{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(".CreateModuleContext<", StringComparison.Ordinal))];
        Assert.Equal(29, files.Length);
        foreach (string path in files) {
            string source = File.ReadAllText(path);
            Assert.Contains("GetRequiredService<IModuleContextFactory>()", source, StringComparison.Ordinal);
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
            Assert.DoesNotContain(root.DescendantNodes().OfType<IdentifierNameSyntax>(),
                identifier => identifier.Identifier.ValueText.Equals("FoodDiaryDbContext", StringComparison.Ordinal));
            InvocationExpressionSyntax creation = Assert.Single(root
                .DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CreateModuleContext" });
            MemberAccessExpressionSyntax access = Assert.IsType<MemberAccessExpressionSyntax>(creation.Expression);
            Assert.True(access.Expression.ToString().Contains("GetRequiredService<IModuleContextFactory>()", StringComparison.Ordinal)
                || access.Expression is IdentifierNameSyntax { Identifier.ValueText: "factory" }, path);
        }
    }

    [Theory]
    [InlineData("Gamification", "ModuleRegistration.cs")]
    [InlineData("Images", "DependencyInjection.cs")]
    [InlineData("Notifications", "ModuleRegistration.cs")]
    public void OutboxRegistration_UsesScopeGuardWithoutConcreteSharedContext(string module, string fileName) {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", module, "Infrastructure", fileName));
        IEnumerable<IdentifierNameSyntax> identifiers = CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<IdentifierNameSyntax>();
        Assert.Contains(identifiers, identifier => identifier.Identifier.ValueText.Equals("IModuleScopeGuard", StringComparison.Ordinal));
        Assert.DoesNotContain(identifiers, identifier => identifier.Identifier.ValueText.Equals("FoodDiaryDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void AiRegistration_KeepsIndependentOptionsSeparateFromCoordinatedContexts() {
        string source = File.ReadAllText(ArchitectureTestPaths.FromRoot("Modules", "Ai", "Infrastructure", "ModuleRegistration.cs"));
        Assert.Contains("GetRequiredService<IIndependentModuleContextOptionsFactory>()", source, StringComparison.Ordinal);
        Assert.Contains(".CreateOptions<AiDbContext>()", source, StringComparison.Ordinal);
        Assert.Contains(".CreateModuleContext<AiDbContext>", source, StringComparison.Ordinal);
    }
}
