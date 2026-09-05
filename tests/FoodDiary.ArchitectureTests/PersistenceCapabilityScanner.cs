using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
internal static class PersistenceCapabilityScanner {
    private static readonly HashSet<string> MappedEntityTypes = typeof(FoodDiaryDbContext)
        .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(property => property.PropertyType.GenericTypeArguments[0].FullName!)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly string[] WriteMethods = [
        "Add", "AddAsync", "AddRange", "AddRangeAsync", "Attach", "AttachRange", "Update", "UpdateRange",
        "Remove", "RemoveRange", "ExecuteDelete", "ExecuteDeleteAsync", "ExecuteUpdate", "ExecuteUpdateAsync",
    ];

    public static IReadOnlyDictionary<string, string[]> Scan(IEnumerable<(string Path, string Source)> sources) {
        SyntaxTree[] trees = [.. sources.Select(source => CSharpSyntaxTree.ParseText(source.Source, path: source.Path))];
        MetadataReference[] references = [.. Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
            .Concat(((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))];
        var compilation = CSharpCompilation.Create(
            "PersistenceCapabilityReview", trees.Append(CSharpSyntaxTree.ParseText(
                "global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;")), references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All));
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees) {
            SemanticModel model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var capabilities = new HashSet<string>(StringComparer.Ordinal);
            SyntaxNode root = tree.GetRoot();
            foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()
                         .Where(identifier => identifier.Identifier.ValueText.Equals("FoodDiaryDbContext", StringComparison.Ordinal))) {
                if (model.GetTypeInfo(identifier).Type is IErrorTypeSymbol) {
                    capabilities.Add("unresolved:FoodDiaryDbContext");
                }
            }
            foreach (MemberAccessExpressionSyntax access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>()) {
                if (model.GetTypeInfo(access).Type is INamedTypeSymbol accessedEntity && IsEntity(accessedEntity)) {
                    capabilities.Add($"entity:{accessedEntity.Name}");
                }
                ITypeSymbol? receiver = model.GetTypeInfo(access.Expression).Type;
                if (!IsContext(receiver)) { continue; }
                string? entity = QueryEntity(model.GetTypeInfo(access).Type);
                if (entity is not null) {
                    capabilities.Add($"entity:{entity}");
                    if (!HasNoTracking(QueryChain(access))) {
                        capabilities.Add($"tracked:{entity}");
                    }
                } else if (model.GetSymbolInfo(access).Symbol is IPropertySymbol) {
                    capabilities.Add($"context:{access.Name.Identifier.ValueText}");
                }
            }
            foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
                if (invocation.Expression is not MemberAccessExpressionSyntax access) { continue; }
                ITypeSymbol? receiver = model.GetTypeInfo(access.Expression).Type;
                string methodName = access.Name.Identifier.ValueText;
                var method = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (IsContext(receiver)) {
                    if (methodName.Equals("Set", StringComparison.Ordinal)) {
                        string? entity = QueryEntity(model.GetTypeInfo(invocation).Type);
                        if (entity is null) {
                            capabilities.Add("unresolved:Set");
                        } else {
                            capabilities.Add($"entity:{entity}");
                            if (!HasNoTracking(QueryChain(invocation))) {
                                capabilities.Add($"tracked:{entity}");
                            }
                        }
                    } else {
                        capabilities.Add($"context:{methodName}");
                    }
                }
                string? queryEntity = QueryEntity(receiver);
                // Include static extension syntax as well as reduced extension calls and aliases.
                if (queryEntity is null && method?.IsExtensionMethod == true && method.ReducedFrom is null && invocation.ArgumentList.Arguments.Count > 0) {
                    queryEntity = QueryEntity(model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type);
                }
                if (queryEntity is not null) {
                    capabilities.Add($"entity:{queryEntity}");
                    if (!HasNoTracking(QueryChain(invocation))) {
                        capabilities.Add($"tracked:{queryEntity}");
                    }
                }
                if (queryEntity is not null && WriteMethods.Contains(methodName, StringComparer.Ordinal)) {
                    capabilities.Add($"write:{queryEntity}");
                }
                if (queryEntity is not null && methodName.Equals("AsTracking", StringComparison.Ordinal)) {
                    capabilities.Add($"tracked:{queryEntity}");
                }
                if (receiver is INamedTypeSymbol entityType && IsEntity(entityType)) {
                    capabilities.Add($"call:{entityType.Name}.{methodName}");
                }
            }
            foreach (AssignmentExpressionSyntax assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>()) {
                if (assignment.Left is MemberAccessExpressionSyntax access && model.GetTypeInfo(access.Expression).Type is INamedTypeSymbol entityType && IsEntity(entityType)) {
                    capabilities.Add($"write:{entityType.Name}");
                }
            }
            if (capabilities.Count > 0) {
                result.Add(tree.FilePath.Replace('\\', '/'), [.. capabilities.Order(StringComparer.Ordinal)]);
            }
        }
        return result;
    }

    private static bool IsContext(ITypeSymbol? type) {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType) {
            if (current.ToDisplayString().Equals("Microsoft.EntityFrameworkCore.DbContext", StringComparison.Ordinal)) { return true; }
        }
        return false;
    }

    private static string? QueryEntity(ITypeSymbol? type) {
        if (type is not INamedTypeSymbol named) { return null; }
        foreach (INamedTypeSymbol candidate in named.AllInterfaces.Prepend(named)) {
            if (candidate is { Name: "IQueryable" or "DbSet", TypeArguments.Length: 1 } && candidate.TypeArguments[0] is INamedTypeSymbol entity && IsEntity(entity)) {
                return entity.Name;
            }
        }
        return null;
    }

    private static bool IsEntity(INamedTypeSymbol type) =>
        MappedEntityTypes.Contains(type.ToDisplayString())
        || type.ContainingNamespace.ToDisplayString().StartsWith("FoodDiary.Domain.Entities.", StringComparison.Ordinal)
        || type.Name.EndsWith("OutboxMessage", StringComparison.Ordinal)
        || type.Name.Equals("AuditEntry", StringComparison.Ordinal);

    private static bool HasNoTracking(SyntaxNode node) {
        while (true) {
            switch (node) {
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AsNoTracking" or "AsNoTrackingWithIdentityResolution" } }:
                    return true;
                case InvocationExpressionSyntax invocation:
                    node = invocation.Expression;
                    break;
                case MemberAccessExpressionSyntax access:
                    node = access.Expression;
                    break;
                default:
                    return false;
            }
        }
    }

    private static SyntaxNode QueryChain(SyntaxNode node) {
        while (node.Parent is MemberAccessExpressionSyntax or InvocationExpressionSyntax) { node = node.Parent; }
        return node;
    }
}
