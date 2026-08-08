using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionTreeSpanOverloadAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0010";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Avoid span overloads in expression trees",
        "Call '{0}' explicitly through its non-span overload because interpreted expression trees cannot execute span overloads",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var method = context.SemanticModel
            .GetSymbolInfo(invocation, context.CancellationToken)
            .Symbol as IMethodSymbol;

        if (method is null ||
            !string.Equals(method.ContainingType.ToDisplayString(), "System.MemoryExtensions", StringComparison.Ordinal) ||
            !IsInsideExpressionTree(invocation, context.SemanticModel, context.CancellationToken)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Expression.GetLocation(),
            method.Name));
    }

    private static bool IsInsideExpressionTree(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken) {
        foreach (AnonymousFunctionExpressionSyntax lambda in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>()) {
            ITypeSymbol? convertedType = semanticModel.GetTypeInfo(lambda, cancellationToken).ConvertedType;
            if (convertedType is INamedTypeSymbol {
                    OriginalDefinition.Name: "Expression",
                    OriginalDefinition.ContainingNamespace: { } containingNamespace,
                } &&
                string.Equals(containingNamespace.ToDisplayString(), "System.Linq.Expressions", StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }
}
