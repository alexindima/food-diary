using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HashSetCollectionExpressionAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0021";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use a collection expression for HashSet",
        "Use a collection expression with 'with(collection, comparer)' to retain the HashSet constructor",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start => {
            INamedTypeSymbol? hashSet = start.Compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
            INamedTypeSymbol? stringComparer = start.Compilation.GetTypeByMetadataName("System.StringComparer");
            if (hashSet is not null && stringComparer is not null) {
                start.RegisterSyntaxNodeAction(
                    node => AnalyzeCreation(node, hashSet, stringComparer),
                    SyntaxKind.ObjectCreationExpression,
                    SyntaxKind.ImplicitObjectCreationExpression);
            }
        });
    }

    private static void AnalyzeCreation(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol hashSet,
        INamedTypeSymbol stringComparer) {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;
        if (creation.SyntaxTree.Options is not CSharpParseOptions { LanguageVersion: > LanguageVersion.CSharp14 } ||
            creation is not {
                ArgumentList.Arguments.Count: 2,
                Initializer: null,
                Parent: EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } },
            } ||
            declaration.Type.IsVar) {
            return;
        }

        SemanticModel model = context.SemanticModel;
        if (model.GetOperation(creation, context.CancellationToken) is not IObjectCreationOperation {
            Constructor: { } constructor,
            Type: INamedTypeSymbol { Arity: 1 } type,
            Arguments.Length: 2,
        } operation ||
            constructor.Parameters[0].Type.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T ||
            type.TypeArguments[0].SpecialType != SpecialType.System_String ||
            !SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, hashSet) ||
            !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(declaration.Type, context.CancellationToken).Type, type)) {
            return;
        }

        int collectionIndex = operation.Arguments[0].Parameter?.Ordinal == 0 ? 0 : 1;
        IArgumentOperation comparer = operation.Arguments[1 - collectionIndex];

        if (comparer.Syntax is ArgumentSyntax { Expression: var comparison } &&
            model.GetSymbolInfo(comparison, context.CancellationToken).Symbol is IPropertySymbol {
                IsStatic: true,
                Name: "Ordinal" or "OrdinalIgnoreCase",
            } property &&
            SymbolEqualityComparer.Default.Equals(property.ContainingType, stringComparer)) {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.NewKeyword.GetLocation()));
        }
    }
}
