using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantEmptyRecordParameterListAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0011";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Remove the redundant empty record parameter list",
        "Record '{0}' has a redundant empty parameter list",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeRecord, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeRecord(SyntaxNodeAnalysisContext context) {
        var declaration = (RecordDeclarationSyntax)context.Node;
        if (declaration.ParameterList is not { Parameters.Count: 0 } parameterList) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            parameterList.GetLocation(),
            declaration.Identifier.ValueText));
    }
}
