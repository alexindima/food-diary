using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrimaryConstructorBackingFieldAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0012";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use the primary constructor parameter directly",
        "Field '{0}' only stores primary constructor parameter '{1}'; use the parameter directly",
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
            !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) ||
            field.Declaration.Variables.Count != 1 ||
            field.Parent is not ClassDeclarationSyntax { ParameterList: { } parameterList } classDeclaration ||
            classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword)) {
            return;
        }

        VariableDeclaratorSyntax variable = field.Declaration.Variables[0];
        if (variable.Initializer?.Value is not IdentifierNameSyntax initializer ||
            !parameterList.Parameters.Any(parameter => string.Equals(
                parameter.Identifier.ValueText,
                initializer.Identifier.ValueText,
                System.StringComparison.Ordinal))) {
            return;
        }

        string parameterName = initializer.Identifier.ValueText;
        bool initializesAnotherField = classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(otherField => !ReferenceEquals(otherField, field))
            .SelectMany(otherField => otherField.Declaration.Variables)
            .Any(otherVariable => otherVariable.Initializer?.Value
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(identifier => string.Equals(
                    identifier.Identifier.ValueText,
                    parameterName,
                    System.StringComparison.Ordinal)) == true);
        if (initializesAnotherField) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            variable.Identifier.GetLocation(),
            variable.Identifier.ValueText,
            parameterName));
    }
}
