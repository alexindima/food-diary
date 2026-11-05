using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamespaceQualifiedTypeAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0020";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Import namespaces instead of qualifying type references",
        "Use '{0}' with a using directive for '{1}', or a using alias if names conflict",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            static context => AnalyzeName(context, ((QualifiedNameSyntax)context.Node).Left),
            SyntaxKind.QualifiedName);
        context.RegisterSyntaxNodeAction(
            static context => AnalyzeName(context, ((AliasQualifiedNameSyntax)context.Node).Alias),
            SyntaxKind.AliasQualifiedName);
        context.RegisterSyntaxNodeAction(
            static context => AnalyzeName(context, ((MemberAccessExpressionSyntax)context.Node).Expression),
            SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeName(SyntaxNodeAnalysisContext context, SyntaxNode qualifier) {
        SyntaxNode node = context.Node;
        if (node.Ancestors().Any(ancestor => ancestor is UsingDirectiveSyntax or DocumentationCommentTriviaSyntax ||
                (ancestor is BaseNamespaceDeclarationSyntax declaration && declaration.Name.Span.Contains(node.Span)))) {
            return;
        }

        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;
        INamedTypeSymbol? type = symbol switch {
            INamedTypeSymbol namedType => namedType,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType,
            _ => null,
        };

        if (type is null || type.TypeKind == TypeKind.Error ||
            context.SemanticModel.GetSymbolInfo(qualifier, context.CancellationToken).Symbol is not INamespaceSymbol { IsGlobalNamespace: false } containingNamespace ||
            UsesAlias(qualifier, context)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            node.GetLocation(),
            type.Name,
            containingNamespace.ToDisplayString()));
    }

    private static bool UsesAlias(SyntaxNode qualifier, SyntaxNodeAnalysisContext context) {
        while (true) {
            switch (qualifier) {
                case QualifiedNameSyntax name:
                    qualifier = name.Left;
                    break;
                case MemberAccessExpressionSyntax member:
                    qualifier = member.Expression;
                    break;
                case AliasQualifiedNameSyntax name:
                    qualifier = name.Alias;
                    break;
                default:
                    return qualifier is IdentifierNameSyntax identifier &&
                        !string.Equals(identifier.Identifier.ValueText, "global", StringComparison.Ordinal) &&
                        context.SemanticModel.GetAliasInfo(identifier, context.CancellationToken) is not null;
            }
        }
    }
}
