using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IgnoredResultAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0019";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, "Handle operation failures",
        "Handle or propagate this Result; do not discard operation failures",
        "Reliability", DiagnosticSeverity.Warning, isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeStatement, OperationKind.ExpressionStatement);
    }

    private static void AnalyzeStatement(OperationAnalysisContext context) {
        IOperation operation = ((IExpressionStatementOperation)context.Operation).Operation;
        if (operation is ISimpleAssignmentOperation assignment) {
            if (assignment.Target is not IDiscardOperation) { return; }
            operation = assignment.Value;
        }
        if (IsResult(operation.Type)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation()));
        }
    }

    private static bool IsResult(ITypeSymbol? type) {
        if (type is not INamedTypeSymbol named) { return false; }
        string name = named.OriginalDefinition.ToDisplayString();
        if (named.TypeArguments.Length == 1 && name is
            "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>" or
            "System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult>" or
            "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult>") {
            return IsResult(named.TypeArguments[0]);
        }
        for (INamedTypeSymbol? current = named; current is not null; current = current.BaseType) {
            if (string.Equals(current.ToDisplayString(), "FoodDiary.Results.Result", StringComparison.Ordinal)) { return true; }
        }
        return false;
    }
}
