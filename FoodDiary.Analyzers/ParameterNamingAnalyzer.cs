using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterNamingAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0013";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use camel case for parameters",
        "Parameter '{0}' must use camel case",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context) {
        var parameter = (ParameterSyntax)context.Node;
        string name = parameter.Identifier.ValueText;
        if (name.Length == 0 || !char.IsUpper(name[0]) ||
            parameter.Parent?.Parent is RecordDeclarationSyntax) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Identifier.GetLocation(), name));
    }
}
