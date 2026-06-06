using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class CSharpSyntaxReader {
    public static IReadOnlyList<MethodDeclaration> ReadMethods(string path) {
        string source = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(method => new MethodDeclaration(
                path,
                tree.GetLineSpan(method.Span).StartLinePosition.Line + 1,
                method.ReturnType.ToString(),
                method.Identifier.ValueText,
                method.ParameterList.Parameters.ToString(),
                method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                IsControllerAction(method)))
            .ToArray();
    }

    public static string? ReadNamespace(string path) {
        string source = File.ReadAllText(path);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
            .FirstOrDefault();
    }

    public static IReadOnlyList<SourceLocation> ReadTargetTypedNewInvocationArguments(string path) {
        string source = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<ImplicitObjectCreationExpressionSyntax>()
            .Where(static expression =>
                expression.Parent is ArgumentSyntax {
                    Parent: ArgumentListSyntax {
                        Parent: InvocationExpressionSyntax
                    }
                })
            .Select(expression => new SourceLocation(
                path,
                tree.GetLineSpan(expression.Span).StartLinePosition.Line + 1,
                expression.ToString()))
            .ToArray();
    }

    private static bool IsControllerAction(MethodDeclarationSyntax method) {
        if (method.Parent is not ClassDeclarationSyntax classDeclaration) {
            return false;
        }

        return classDeclaration.Identifier.ValueText.EndsWith("Controller", StringComparison.Ordinal) ||
               classDeclaration.Identifier.ValueText.EndsWith("ControllerBase", StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    internal sealed record SourceLocation(string Path, int Line, string Text) {
        public string Format(string repositoryRoot) =>
            $"{System.IO.Path.GetRelativePath(repositoryRoot, Path)}:{Line} {Text}";
    }

    [ExcludeFromCodeCoverage]
    internal sealed record MethodDeclaration(
        string Path,
        int Line,
        string ReturnType,
        string Name,
        string Parameters,
        bool HasAsyncModifier,
        bool IsControllerAction) {
        public bool IsAsyncLike {
            get {
                string normalizedReturnType = ReturnType.Replace(" ", string.Empty, StringComparison.Ordinal);

                return HasAsyncModifier ||
                    normalizedReturnType.Equals("Task", StringComparison.Ordinal) ||
                    normalizedReturnType.Equals("ValueTask", StringComparison.Ordinal) ||
                    normalizedReturnType.EndsWith(".Task", StringComparison.Ordinal) ||
                    normalizedReturnType.EndsWith(".ValueTask", StringComparison.Ordinal) ||
                    normalizedReturnType.StartsWith("Task<", StringComparison.Ordinal) ||
                    normalizedReturnType.StartsWith("ValueTask<", StringComparison.Ordinal) ||
                    normalizedReturnType.Contains(".Task<", StringComparison.Ordinal) ||
                    normalizedReturnType.Contains(".ValueTask<", StringComparison.Ordinal);
            }
        }

        public string Format(string repositoryRoot) =>
            $"{System.IO.Path.GetRelativePath(repositoryRoot, Path)}:{Line} {ReturnType} {Name}({Parameters})";
    }
}
