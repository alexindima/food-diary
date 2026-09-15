using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class QueryReadBoundaryScanner {
    public static string[] FindViolations(IEnumerable<(string Path, string Source)> sources) {
        SyntaxTree[] trees = [.. sources.Select(source => CSharpSyntaxTree.ParseText(source.Source, path: source.Path))];
        MetadataReference[] references = [.. Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
            .Concat(((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create("QueryBoundaryReview", trees.Append(CSharpSyntaxTree.ParseText(
            "global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;")), references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All));
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees) {
            SemanticModel model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes().Where(node => node is InvocationExpressionSyntax or TypeSyntax)) {
                ITypeSymbol? type = model.GetTypeInfo(node).Type;
                if (ContainsEntity(type)) {
                    violations.Add($"{tree.FilePath}:{(tree.GetLineSpan(node.Span).StartLinePosition.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}: query uses aggregate {type}");
                }
            }
        }
        return [.. violations];
    }

    private static bool ContainsEntity(ITypeSymbol? type) => type switch {
        IArrayTypeSymbol array => ContainsEntity(array.ElementType),
        INamedTypeSymbol named => named.ContainingNamespace.ToDisplayString().Split('.') is
            ["FoodDiary", "Domain", "Entities", ..] or ["FoodDiary", "Modules", _, "Domain", "Entities", ..]
            || named.TypeArguments.Any(ContainsEntity),
        _ => false,
    };
}
