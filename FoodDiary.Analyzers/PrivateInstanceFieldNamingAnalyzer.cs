using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrivateInstanceFieldNamingAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0014";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use underscore camel case for private instance fields",
        "Private instance field '{0}' must use underscore camel case",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context) {
        var field = (FieldDeclarationSyntax)context.Node;
        if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword) ||
            field.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            field.Modifiers.Any(SyntaxKind.ConstKeyword)) {
            return;
        }

        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables) {
            string name = variable.Identifier.ValueText;
            if (name.Length >= 2 && name[0] == '_' && char.IsLower(name[1])) {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, variable.Identifier.GetLocation(), name));
        }
    }
}
