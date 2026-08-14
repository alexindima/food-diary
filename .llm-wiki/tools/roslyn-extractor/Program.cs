using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string[] paths;
string[] contextPaths;
bool semantic = true;
if (args.Length == 1 && args[0] == "--stdin") {
    using JsonDocument input = JsonDocument.Parse(Console.In.ReadToEnd());
    if (input.RootElement.ValueKind == JsonValueKind.Array) {
        paths = input.RootElement.Deserialize<string[]>() ?? [];
        contextPaths = paths;
    } else {
        paths = input.RootElement.GetProperty("paths").Deserialize<string[]>() ?? [];
        contextPaths = input.RootElement.TryGetProperty("contextPaths", out JsonElement context)
            ? context.Deserialize<string[]>() ?? paths
            : paths;
        semantic = !input.RootElement.TryGetProperty("semantic", out JsonElement semanticElement) || semanticElement.GetBoolean();
    }
} else {
    paths = args;
    contextPaths = paths;
}
if (paths.Length == 0) {
    Console.Error.WriteLine("Pass one or more C# source paths.");
    return 2;
}

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
Dictionary<string, SyntaxTree> trees = contextPaths
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToDictionary(path => path, path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path), StringComparer.OrdinalIgnoreCase);
IEnumerable<MetadataReference> references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
    .Select(path => MetadataReference.CreateFromFile(path));
var results = new List<ExtractionResult>();
if (!semantic) {
    results.AddRange(paths.Select(path => Extract(path, trees[path], null, null)));
} else {
    HashSet<string> targetPaths = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, string> projectKeys = contextPaths.Distinct(StringComparer.OrdinalIgnoreCase)
        .ToDictionary(path => path, ProjectKey, StringComparer.OrdinalIgnoreCase);
    foreach (IGrouping<string, string> group in paths.GroupBy(path => projectKeys[path], StringComparer.OrdinalIgnoreCase)) {
        string[] groupTargets = group.ToArray();
        SyntaxTree[] groupTrees = trees
            .Where(item => string.Equals(projectKeys[item.Key], group.Key, StringComparison.OrdinalIgnoreCase) || !targetPaths.Contains(item.Key))
            .Select(item => item.Value)
            .ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create($"LlmWiki.SemanticGraph.{results.Count}", groupTrees, references);
        results.AddRange(groupTargets.Select(path => Extract(path, trees[path], compilation, group.Key)));
    }
}
Console.WriteLine(JsonSerializer.Serialize(results, options));
return 0;

static ExtractionResult Extract(string path, SyntaxTree tree, CSharpCompilation? compilation, string? projectRoot) {
    var root = tree.GetRoot();
    SemanticModel? semanticModel = compilation?.GetSemanticModel(tree, ignoreAccessibility: true);
    var symbols = new List<GraphSymbol>();
    var edges = new List<GraphEdge>();

    BaseNamespaceDeclarationSyntax? namespaceDeclaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
    if (namespaceDeclaration is not null) {
        string declaredNamespace = namespaceDeclaration.Name.ToString();
        AddEdge(edges, "declared-namespace", declaredNamespace, namespaceDeclaration, "high");
        string? expectedNamespace = ExpectedNamespace(path, projectRoot);
        if (expectedNamespace is not null && !string.Equals(declaredNamespace, expectedNamespace, StringComparison.Ordinal)) {
            AddEdge(edges, "namespace-path-mismatch", declaredNamespace, namespaceDeclaration, "high", expectedNamespace);
        }
    }

    foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>()) {
        var symbol = declaration switch {
            BaseTypeDeclarationSyntax type => new GraphSymbol(TypeKind(type), type.Identifier.ValueText, TokenLine(type.Identifier)),
            DelegateDeclarationSyntax @delegate => new GraphSymbol("delegate", @delegate.Identifier.ValueText, TokenLine(@delegate.Identifier)),
            MethodDeclarationSyntax method => new GraphSymbol("method", method.Identifier.ValueText, TokenLine(method.Identifier)),
            ConstructorDeclarationSyntax constructor => new GraphSymbol("constructor", constructor.Identifier.ValueText, TokenLine(constructor.Identifier)),
            _ => null,
        };
        if (symbol is not null) {
            ISymbol? declared = semanticModel?.GetDeclaredSymbol(declaration);
            symbols.Add(symbol with { SymbolId = SymbolId(declared) ?? SyntacticSymbolId(declaration) });
        }
    }

    foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>()) {
        if (directive.Name is not null) AddEdge(edges, "namespace-import", directive.Name.ToString(), directive, "high");
    }
    foreach (var baseType in root.DescendantNodes().OfType<BaseTypeSyntax>()) {
        AddEdge(edges, "type-inheritance", baseType.Type.ToString(), baseType, "high");
        ITypeSymbol? resolvedBaseType = semanticModel?.GetTypeInfo(baseType.Type).Type;
        if (resolvedBaseType is not null) AddEdge(edges, "resolved-type-inheritance", baseType.Type.ToString(), baseType, "high", SymbolId(resolvedBaseType));
        if (baseType.Type is GenericNameSyntax generic && IsMediatorHandler(generic.Identifier.ValueText)) {
            foreach (var argument in generic.TypeArgumentList.Arguments) AddEdge(edges, "mediator-handler", argument.ToString(), argument, "high");
        }
    }
    foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
        var name = InvocationName(invocation.Expression);
        if (name is null) continue;
        AddEdge(edges, "method-call", name, invocation, "high");
        IMethodSymbol? resolvedMethod = semanticModel?.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (resolvedMethod is not null) AddEdge(edges, "resolved-method-call", name, invocation, "high", SymbolId(resolvedMethod));
        if (name is "AddScoped" or "AddTransient" or "AddSingleton" or "AddKeyedScoped" or "AddKeyedTransient" or "AddKeyedSingleton") {
            var generic = invocation.Expression.DescendantNodesAndSelf().OfType<GenericNameSyntax>().LastOrDefault();
            if (generic is not null) {
                var arguments = generic.TypeArgumentList.Arguments;
                if (arguments.Count > 0) AddEdge(edges, "di-service", arguments[0].ToString(), invocation, "high");
                if (arguments.Count > 1) AddEdge(edges, "di-implementation", arguments[1].ToString(), invocation, "high");
            }
        } else if (name is "Send" or "Publish") {
            var created = invocation.ArgumentList.Arguments.SelectMany(argument => argument.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>()).FirstOrDefault();
            if (created is not null) AddEdge(edges, "mediator-dispatch", created.Type.ToString(), invocation, "high");
        } else if (name is "MapGet" or "MapPost" or "MapPut" or "MapPatch" or "MapDelete") {
            AddFirstStringArgument(edges, "http-route", invocation);
        } else if (name is "CreateTable" or "DropTable" or "RenameTable") {
            AddNamedStringArgument(edges, "migration-table", invocation, "name");
        } else if (name is "AddColumn" or "DropColumn" or "RenameColumn" or "AlterColumn") {
            AddNamedStringArgument(edges, "migration-column", invocation, "table");
        }
    }
    foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()) {
        AddEdge(edges, "type-construction", creation.Type.ToString(), creation, "high");
        IMethodSymbol? constructor = semanticModel?.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        if (constructor?.ContainingType is not null) AddEdge(edges, "resolved-type-construction", creation.Type.ToString(), creation, "high", SymbolId(constructor.ContainingType));
    }
    foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>()) {
        var name = attribute.Name.ToString();
        if (name is "HttpGet" or "HttpPost" or "HttpPut" or "HttpPatch" or "HttpDelete" || name.EndsWith("Attribute", StringComparison.Ordinal) && name.StartsWith("Http", StringComparison.Ordinal)) {
            var value = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax;
            AddEdge(edges, "http-attribute", value?.Token.ValueText ?? name.Replace("Attribute", string.Empty, StringComparison.Ordinal), attribute, "high");
        }
    }
    foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(item => item.IsKind(SyntaxKind.StringLiteralExpression))) {
        string value = literal.Token.ValueText;
        if (!LooksLikeQualifiedName(value)) continue;
        AddEdge(edges, "qualified-name-literal", value, literal, "medium");
        InvocationExpressionSyntax? invocation = literal.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation is not null && invocation.ToString().Contains("Namespace", StringComparison.Ordinal)) {
            AddEdge(edges, "namespace-filter", value.TrimEnd('*').TrimEnd('.'), invocation, "high");
        }
    }

    var tokens = root.DescendantTokens()
        .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
        .Select(token => token.ValueText)
        .Where(token => token.Length >= 3 && (char.IsUpper(token[0]) || token.EndsWith("Async", StringComparison.Ordinal)))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    return new ExtractionResult(path.Replace('\\', '/'), symbols, tokens, edges);
}

static string TypeKind(BaseTypeDeclarationSyntax declaration) => declaration switch {
    ClassDeclarationSyntax => "class",
    InterfaceDeclarationSyntax => "interface",
    RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
    RecordDeclarationSyntax => "record",
    StructDeclarationSyntax => "struct",
    EnumDeclarationSyntax => "enum",
    _ => "type",
};

static string ProjectKey(string path) {
    string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
    while (!string.IsNullOrWhiteSpace(directory)) {
        if (Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any()) return directory;
        directory = Path.GetDirectoryName(directory);
    }
    return Path.GetDirectoryName(path) ?? string.Empty;
}

static string? ExpectedNamespace(string path, string? projectRoot) {
    if (string.IsNullOrWhiteSpace(projectRoot)) return null;
    string? projectFile = Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly).SingleOrDefault();
    string projectName = projectFile is null ? Path.GetFileName(projectRoot) : Path.GetFileNameWithoutExtension(projectFile);
    string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
    if (directory is null) return projectName;
    string relative = Path.GetRelativePath(projectRoot, directory);
    return relative == "." ? projectName : $"{projectName}.{relative.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.')}";
}

static bool LooksLikeQualifiedName(string value) {
    string normalized = value.TrimEnd('*').TrimEnd('.');
    string[] segments = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
    return segments.Length >= 3 && segments.All(segment => SyntaxFacts.IsValidIdentifier(segment));
}

static bool IsMediatorHandler(string name) => name is "IRequestHandler" or "ICommandHandler" or "IQueryHandler" or "INotificationHandler";
static string? InvocationName(ExpressionSyntax expression) => expression switch {
    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
    GenericNameSyntax generic => generic.Identifier.ValueText,
    MemberAccessExpressionSyntax member => InvocationName(member.Name),
    _ => expression.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().LastOrDefault()?.Identifier.ValueText,
};
static int TokenLine(SyntaxToken token) => token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
static int NodeLine(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
static string? SymbolId(ISymbol? symbol) => symbol switch {
    null => null,
    IMethodSymbol method => $"{SymbolId(method.ContainingType)}.{method.MetadataName}({string.Join(",", method.Parameters.Select(parameter => parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))})",
    _ => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
};
static string? SyntacticSymbolId(MemberDeclarationSyntax declaration) {
    string? name = declaration switch {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        MethodDeclarationSyntax method => method.Identifier.ValueText,
        ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
        _ => null,
    };
    if (name is null) return null;
    string? namespaceName = declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
    string[] containingTypes = declaration.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(type => type.Identifier.ValueText).ToArray();
    string prefix = string.Join(".", new[] { namespaceName }.Where(value => !string.IsNullOrWhiteSpace(value)).Concat(containingTypes));
    return $"global::{(string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix}.{name}")}";
}
static void AddEdge(List<GraphEdge> edges, string kind, string? target, SyntaxNode node, string confidence, string? targetId = null) {
    if (!string.IsNullOrWhiteSpace(target)) edges.Add(new GraphEdge(kind, target, NodeLine(node), node.ToString().Trim(), confidence, targetId));
}
static void AddFirstStringArgument(List<GraphEdge> edges, string kind, InvocationExpressionSyntax invocation) {
    var literal = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax;
    AddEdge(edges, kind, literal?.Token.ValueText, invocation, "high");
}
static void AddNamedStringArgument(List<GraphEdge> edges, string kind, InvocationExpressionSyntax invocation, string name) {
    var argument = invocation.ArgumentList.Arguments.FirstOrDefault(item => item.NameColon?.Name.Identifier.ValueText == name);
    AddEdge(edges, kind, (argument?.Expression as LiteralExpressionSyntax)?.Token.ValueText, invocation, "high");
}

sealed record ExtractionResult(string Path, IReadOnlyList<GraphSymbol> Symbols, IReadOnlyList<string> Tokens, IReadOnlyList<GraphEdge> Edges);
sealed record GraphSymbol(string Kind, string Name, int Line, string? SymbolId = null);
sealed record GraphEdge(string Kind, string Target, int Line, string Evidence, string Confidence, string? TargetId = null);
