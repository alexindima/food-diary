using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FoodDiary.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProjectConventionAnalyzer : DiagnosticAnalyzer {
    public const string AsyncSuffixRequiredId = "FD0002";
    public const string AsyncSuffixForbiddenId = "FD0003";
    public const string CancellationTokenRequiredId = "FD0004";
    public const string ExplicitInvocationArgumentTypeId = "FD0005";
    public const string TimeProviderRequiredId = "FD0006";
    public const string TestCoverageExclusionRequiredId = "FD0007";
    public const string ConcreteClassMustBeClosedId = "FD0008";
    public const string ExternalTestConnectionForbiddenId = "FD0009";

    private static readonly HashSet<string> AllowedNonAsyncSuffixNames = new(StringComparer.Ordinal) {
        "Execute",
        "Handle",
        "HandleAccepted",
        "HandleCreated",
        "HandleFile",
        "HandleNoContent",
        "HandleObservedCreated",
        "HandleObservedNoContent",
        "HandleObservedOk",
        "HandleOk",
    };

    private static readonly HashSet<string> FrameworkAsyncHookNames = new(StringComparer.Ordinal) {
        "BindModelAsync",
        "CheckHealthAsync",
        "InvokeAsync",
        "OnActionExecutionAsync",
        "OnAuthorizationAsync",
        "TryHandleAsync",
    };

    private static readonly DiagnosticDescriptor AsyncSuffixRequiredRule = CreateRule(
        AsyncSuffixRequiredId,
        "Use the Async suffix",
        "Async method '{0}' should use the Async suffix");

    private static readonly DiagnosticDescriptor AsyncSuffixForbiddenRule = CreateRule(
        AsyncSuffixForbiddenId,
        "Remove the Async suffix",
        "Synchronous method '{0}' should not use the Async suffix");

    private static readonly DiagnosticDescriptor CancellationTokenRequiredRule = CreateRule(
        CancellationTokenRequiredId,
        "Accept a CancellationToken",
        "Async method '{0}' should accept a CancellationToken");

    private static readonly DiagnosticDescriptor ExplicitInvocationArgumentTypeRule = CreateRule(
        ExplicitInvocationArgumentTypeId,
        "Use an explicit object creation type",
        "Use an explicit type instead of target-typed new() in an invocation argument");

    private static readonly DiagnosticDescriptor TimeProviderRequiredRule = CreateRule(
        TimeProviderRequiredId,
        "Use TimeProvider",
        "Inject TimeProvider instead of reading '{0}.UtcNow' directly");

    private static readonly DiagnosticDescriptor TestCoverageExclusionRequiredRule = CreateRule(
        TestCoverageExclusionRequiredId,
        "Exclude test types from code coverage",
        "Test type '{0}' should be marked with ExcludeFromCodeCoverageAttribute");

    private static readonly DiagnosticDescriptor ConcreteClassMustBeClosedRule = CreateRule(
        ConcreteClassMustBeClosedId,
        "Close concrete classes for inheritance",
        "Concrete class '{0}' should be sealed, static, or abstract");

    private static readonly DiagnosticDescriptor ExternalTestConnectionForbiddenRule = CreateRule(
        ExternalTestConnectionForbiddenId,
        "Do not connect tests to external hosts",
        "Test code should not connect directly to external host '{0}'");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        AsyncSuffixRequiredRule,
        AsyncSuffixForbiddenRule,
        CancellationTokenRequiredRule,
        ExplicitInvocationArgumentTypeRule,
        TimeProviderRequiredRule,
        TestCoverageExclusionRequiredRule,
        ConcreteClassMustBeClosedRule,
        ExternalTestConnectionForbiddenRule,
    ];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.StructDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context) {
        var method = (MethodDeclarationSyntax)context.Node;
        bool isAsyncLike = IsAsyncLike(method);
        string name = method.Identifier.ValueText;

        if (isAsyncLike &&
            !name.EndsWith("Async", StringComparison.Ordinal) &&
            !AllowedNonAsyncSuffixNames.Contains(name) &&
            !IsControllerAction(method)) {
            context.ReportDiagnostic(Diagnostic.Create(AsyncSuffixRequiredRule, method.Identifier.GetLocation(), name));
        }

        if (!isAsyncLike &&
            name.EndsWith("Async", StringComparison.Ordinal) &&
            !FrameworkAsyncHookNames.Contains(name)) {
            context.ReportDiagnostic(Diagnostic.Create(AsyncSuffixForbiddenRule, method.Identifier.GetLocation(), name));
        }

        if (isAsyncLike &&
            !AcceptsCancellationToken(method) &&
            !IsCancellationTokenProvidedByFramework(method)) {
            context.ReportDiagnostic(Diagnostic.Create(CancellationTokenRequiredRule, method.Identifier.GetLocation(), name));
        }
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context) {
        var creation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        if (creation.Parent is ArgumentSyntax {
                Parent: ArgumentListSyntax {
                    Parent: InvocationExpressionSyntax,
                },
            }) {
            context.ReportDiagnostic(Diagnostic.Create(ExplicitInvocationArgumentTypeRule, creation.NewKeyword.GetLocation()));
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context) {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (!memberAccess.Name.Identifier.ValueText.Equals("UtcNow", StringComparison.Ordinal)) {
            return;
        }

        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
        if (symbol?.ContainingType is not { } containingType ||
            containingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                is not ("System.DateTime" or "System.DateTimeOffset")) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            TimeProviderRequiredRule,
            memberAccess.GetLocation(),
            containingType.Name));
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context) {
        var declaration = (TypeDeclarationSyntax)context.Node;
        INamedTypeSymbol? type = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
        if (type is null) {
            return;
        }

        if (!HasExcludeFromCodeCoverage(type)) {
            context.ReportDiagnostic(Diagnostic.Create(
                TestCoverageExclusionRequiredRule,
                declaration.Identifier.GetLocation(),
                declaration.Identifier.ValueText));
        }

        if (declaration is ClassDeclarationSyntax classDeclaration &&
            !IsClosedForInheritance(classDeclaration)) {
            context.ReportDiagnostic(Diagnostic.Create(
                ConcreteClassMustBeClosedRule,
                declaration.Identifier.GetLocation(),
                declaration.Identifier.ValueText));
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            !string.Equals(memberAccess.Name.Identifier.ValueText, "ConnectAsync", StringComparison.Ordinal) ||
            invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not LiteralExpressionSyntax literal ||
            literal.Token.Value is not string host ||
            IsLocalHost(host)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            ExternalTestConnectionForbiddenRule,
            literal.GetLocation(),
            host));
    }

    private static bool IsAsyncLike(MethodDeclarationSyntax method) {
        string returnType = method.ReturnType.ToString().Replace(" ", string.Empty);

        return method.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            returnType.Equals("Task", StringComparison.Ordinal) ||
            returnType.Equals("ValueTask", StringComparison.Ordinal) ||
            returnType.EndsWith(".Task", StringComparison.Ordinal) ||
            returnType.EndsWith(".ValueTask", StringComparison.Ordinal) ||
            returnType.StartsWith("Task<", StringComparison.Ordinal) ||
            returnType.StartsWith("ValueTask<", StringComparison.Ordinal) ||
            returnType.Contains(".Task<", StringComparison.Ordinal) ||
            returnType.Contains(".ValueTask<", StringComparison.Ordinal);
    }

    private static bool AcceptsCancellationToken(MethodDeclarationSyntax method) =>
        method.ParameterList.Parameters.Any(static parameter =>
            parameter.Type?.ToString().Contains("CancellationToken", StringComparison.Ordinal) == true);

    private static bool IsCancellationTokenProvidedByFramework(MethodDeclarationSyntax method) {
        string name = method.Identifier.ValueText;
        string path = method.SyntaxTree.FilePath.Replace('\\', '/');

        return IsControllerAction(method) ||
            name.Equals("DisposeAsync", StringComparison.Ordinal) ||
            name.Equals("Execute", StringComparison.Ordinal) ||
            FrameworkAsyncHookNames.Contains(name) ||
            path.Contains("/Filters/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Middleware/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsControllerAction(MethodDeclarationSyntax method) =>
        method.Parent is ClassDeclarationSyntax controller &&
        (controller.Identifier.ValueText.EndsWith("Controller", StringComparison.Ordinal) ||
         controller.Identifier.ValueText.EndsWith("ControllerBase", StringComparison.Ordinal));

    private static bool HasExcludeFromCodeCoverage(INamedTypeSymbol type) =>
        type.GetAttributes().Any(static attribute =>
            string.Equals(
                attribute.AttributeClass?.ToDisplayString(),
                "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
                StringComparison.Ordinal));

    private static bool IsClosedForInheritance(ClassDeclarationSyntax declaration) =>
        declaration.Modifiers.Any(SyntaxKind.SealedKeyword) ||
        declaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
        declaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
        declaration.SyntaxTree.FilePath.Replace('\\', '/').Contains("/Migrations/", StringComparison.OrdinalIgnoreCase);

    private static bool IsLocalHost(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase) ||
        host.StartsWith("127.", StringComparison.Ordinal);

    private static DiagnosticDescriptor CreateRule(string id, string title, string message) =>
        new(
            id,
            title,
            message,
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false);
}
