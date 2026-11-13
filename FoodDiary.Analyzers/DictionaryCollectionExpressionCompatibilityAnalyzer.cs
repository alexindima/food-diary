using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DictionaryCollectionExpressionCompatibilityAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0022";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Initialize the dictionary in a separate statement",
        "Move this dictionary collection expression out of the {0} to avoid a false conversion error in Rider",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCollectionExpression, SyntaxKind.CollectionExpression);
    }

    private static void AnalyzeCollectionExpression(SyntaxNodeAnalysisContext context) {
        var collection = (CollectionExpressionSyntax)context.Node;
        // Roslyn 5.0 lacks WithElementSyntax, but the C# 15 parser exposes its keyword.
        if (collection.Elements.Count != 1 ||
            !collection.Elements[0].GetFirstToken().IsKind(SyntaxKind.WithKeyword) ||
            collection.SyntaxTree.Options is not CSharpParseOptions { LanguageVersion: > LanguageVersion.CSharp14 }) {
            return;
        }

        SyntaxNode expression = collection;
        while (expression.Parent is ParenthesizedExpressionSyntax parentheses) {
            expression = parentheses;
        }

        bool isConditionalBranch = expression.Parent is ConditionalExpressionSyntax conditional &&
            conditional.Condition != expression;
        bool isArgument = expression.Parent is ArgumentSyntax;
        if ((!isConditionalBranch && !isArgument) ||
            context.SemanticModel.GetTypeInfo(collection, context.CancellationToken).ConvertedType is not INamedTypeSymbol type) {
            return;
        }

        Compilation compilation = context.Compilation;
        INamedTypeSymbol definition = type.OriginalDefinition;
        SymbolEqualityComparer symbols = SymbolEqualityComparer.Default;
        bool isDictionary = symbols.Equals(definition, compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
        if (isDictionary || (isConditionalBranch &&
            (symbols.Equals(definition, compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2")) ||
             symbols.Equals(definition, compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2"))))) {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                collection.OpenBracketToken.GetLocation(),
                isArgument ? "argument list" : "conditional expression"));
        }
    }
}
