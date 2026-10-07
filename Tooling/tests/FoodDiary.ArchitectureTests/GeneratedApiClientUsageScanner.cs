using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class GeneratedApiClientUsageScanner {
    private const string GeneratedRoot = "FoodDiary.Telegram.Bot/Api/Generated/";
    private static readonly MetadataReference[] References = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(path => MetadataReference.CreateFromFile(path))];

    internal static string[] FindViolations(IEnumerable<(string Path, string Source)> sources) {
        SyntaxTree[] trees = [.. sources.Select(source => CSharpSyntaxTree.ParseText(source.Source, path: source.Path.Replace('\\', '/')))];
        SyntaxTree globals = CSharpSyntaxTree.ParseText("global using System; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;");
        var compilation = CSharpCompilation.Create("GeneratedApiUsage", trees.Append(globals), References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var violations = new SortedSet<string>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees.Where(tree => !tree.FilePath.StartsWith(GeneratedRoot, StringComparison.Ordinal))) {
            SemanticModel model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes()) {
                string? reason = null;
                if (node is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax &&
                    model.GetSymbolInfo(node).Symbol is IMethodSymbol bridge && bridge.ContainingType.ToDisplayString() is "FoodDiary.Telegram.Bot.Api.BotApiTransport" &&
                    bridge.Name is "CreateRequest" or "SendAsync") {
                    reason = "Application adapters must call generated methods instead of the low-level API transport.";
                } else if (node is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax &&
                    model.GetSymbolInfo(node).Symbol is IMethodSymbol method && IsHttpDispatch(method) && !AllowedDispatch(tree.FilePath, node, method.Name)) {
                    reason = "Use a generated API method instead of direct HTTP dispatch.";
                } else if (node is BaseObjectCreationExpressionSyntax && model.GetTypeInfo(node).Type?.ToDisplayString() is
                    "System.Net.Http.HttpRequestMessage" or "System.Net.WebClient" && !AllowedRequest(tree.FilePath, node)) {
                    reason = "Handwritten HTTP requests are only allowed in the generated transport or signed storage upload.";
                } else if ((node is LiteralExpressionSyntax literal && literal.Token.ValueText.StartsWith("/api/", StringComparison.Ordinal)) ||
                    (node is InterpolatedStringTextSyntax text && text.TextToken.ValueText.StartsWith("/api/", StringComparison.Ordinal))) {
                    reason = "FoodDiary API routes belong in generated code.";
                }
                if (reason is not null) {
                    int line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
                    violations.Add(string.Create(CultureInfo.InvariantCulture, $"{tree.FilePath}:{line}: {reason}"));
                }
            }
        }
        return [.. violations];
    }

    private static bool IsHttpDispatch(IMethodSymbol method) {
        method = method.ReducedFrom ?? method;
        string type = method.ContainingType.ToDisplayString();
        return type switch {
            "System.Net.Http.HttpClient" or "System.Net.Http.HttpMessageInvoker" => method.Name is
                "Send" or "SendAsync" or "GetAsync" or "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync",
            "System.Net.Http.Json.HttpClientJsonExtensions" => true,
            "System.Net.WebRequest" or "System.Net.HttpWebRequest" => method.Name is "Create" or "CreateHttp" or "GetResponse" or "GetResponseAsync",
            "System.Net.WebClient" => method.Name.StartsWith("Download", StringComparison.Ordinal) || method.Name.StartsWith("Upload", StringComparison.Ordinal),
            _ => false,
        };
    }

    private static (string? Type, string? Method) Owner(SyntaxNode node) => (
        node.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText,
        node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText);

    private static bool AllowedDispatch(string path, SyntaxNode node, string name) {
        if (name is not "SendAsync" || node.Parent is not InvocationExpressionSyntax invocation) {
            return false;
        }
        (string? type, string? method) = Owner(node);
        bool transport = (path, type, method) is ("FoodDiary.Telegram.Bot/Api/BotApiTransport.cs", "BotApiTransport", "SendAsync");
        bool upload = (path, type, method) is ("FoodDiary.Telegram.Bot/Operations/BotDiaryClient.cs", "BotDiaryClient", "UploadAsync");
        return (transport || upload) && invocation.ArgumentList.Arguments.Select(argument => argument.Expression.ToString()).SequenceEqual(
            ["request", "HttpCompletionOption.ResponseHeadersRead", "cancellationToken"], StringComparer.Ordinal);
    }

    private static bool AllowedRequest(string path, SyntaxNode node) {
        if (node is not BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments }) {
            return false;
        }
        (string? type, string? method) = Owner(node);
        string[] values = [.. arguments.Arguments.Select(argument => argument.Expression.ToString())];
        if ((path, type, method) is ("FoodDiary.Telegram.Bot/Api/BotApiTransport.cs", "BotApiTransport", "CreateRequest")) {
            return values.SequenceEqual(["new HttpMethod(method)", "new Uri(baseUri, path)"], StringComparer.Ordinal);
        }
        return (path, type, method) is ("FoodDiary.Telegram.Bot/Operations/BotDiaryClient.cs", "BotDiaryClient", "UploadAsync") &&
            values.SequenceEqual(["HttpMethod.Put", "uri"], StringComparer.Ordinal);
    }
}
