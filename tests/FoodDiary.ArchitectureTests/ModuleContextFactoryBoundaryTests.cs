using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleContextFactoryBoundaryTests {
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
