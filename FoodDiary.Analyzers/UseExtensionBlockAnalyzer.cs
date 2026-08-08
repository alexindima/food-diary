using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseExtensionBlockAnalyzer : DiagnosticAnalyzer {
    public const string DiagnosticId = "FD0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Use a C# extension block",
        "Convert extension method '{0}' to a C# 14 extension block",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Classic extension methods should be declared in C# 14 extension blocks in migrated project scopes.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context) {
        var method = (MethodDeclarationSyntax)context.Node;
        ParameterSyntax? receiver = method.ParameterList.Parameters.FirstOrDefault();
        if (receiver?.Modifiers.Any(SyntaxKind.ThisKeyword) is not true) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText));
    }
}
