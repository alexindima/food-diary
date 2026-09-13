using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RuntimeContextNamespaceTests {
    [Fact]
    public void ModuleContexts_DeclareTheirOwningModuleNamespace() {
        string modulesRoot = ArchitectureTestPaths.FromRoot("Modules");
        var violations = new List<string>();
        int contextCount = 0;
        foreach (string moduleDirectory in Directory.EnumerateDirectories(modulesRoot)) {
            string expected = $"FoodDiary.Modules.{Path.GetFileName(moduleDirectory)}.Infrastructure.Persistence";
            foreach (string path in SourceScanner.SourceFiles(Path.Combine(moduleDirectory, "Infrastructure"))) {
                SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
                foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                             .Where(declaration => declaration.Identifier.ValueText.EndsWith("DbContext", StringComparison.Ordinal)
                                 || declaration.BaseList?.Types.Any(type => type.Type.ToString() is "DbContext" or "Microsoft.EntityFrameworkCore.DbContext") == true)) {
                    contextCount++;
                    string actual = string.Join('.', declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
                        .Reverse().Select(@namespace => @namespace.Name.ToString()));
                    if (!string.Equals(expected, actual, StringComparison.Ordinal)) {
                        violations.Add($"{Path.GetRelativePath(modulesRoot, path)}: {declaration.Identifier.ValueText} uses '{actual}', expected '{expected}'.");
                    }
                }
            }
        }
        Assert.True(contextCount > 0, "No module runtime contexts were discovered.");
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }
}
