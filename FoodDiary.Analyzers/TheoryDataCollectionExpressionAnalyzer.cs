using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TheoryDataCollectionExpressionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0017";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use a collection expression for TheoryData",
        "Use a collection expression instead of a collection initializer for TheoryData",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeObjectCreation,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context) {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;
        if (creation.Initializer?.IsKind(SyntaxKind.CollectionInitializerExpression) != true) {
            return;
        }

        if (context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type is not INamedTypeSymbol type ||
            !IsTheoryData(type)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, creation.NewKeyword.GetLocation()));
    }

    private static bool IsTheoryData(INamedTypeSymbol type) =>
        string.Equals(type.Name, "TheoryData", StringComparison.Ordinal) &&
        type.Arity == 1 &&
        string.Equals(type.ContainingNamespace.ToDisplayString(), "Xunit", StringComparison.Ordinal);
}
