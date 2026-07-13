using System.Reflection;
using System.Diagnostics;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Globalization;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ControllerConventionsTests {
    private static readonly Assembly PresentationAssembly = typeof(Controllers.BaseApiController).Assembly;

    [Fact]
    public void FeatureControllers_HaveApiControllerAttribute() {
        string?[] violations = [.. GetFeatureControllerTypes()
            .Where(type => type.GetCustomAttribute<ApiControllerAttribute>() is null)
            .Select(type => type.FullName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_ReturnTaskOfActionResult() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => !IsNonStandardInfrastructureController(method.DeclaringType))
            .Where(method => method.ReturnType != typeof(Task<IActionResult>))
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotExposeMoreThanEightActions() {
        string[] violations = [.. GetFeatureControllerTypes()
            .Select(type => new {
                Type = type,
                ActionCount = GetActionMethods(type).Length,
            })
            .Where(static entry => entry.ActionCount > 8)
            .Select(entry => string.Create(CultureInfo.InvariantCulture, $"{entry.Type.FullName} ({entry.ActionCount} actions)"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_DeclareProducesResponseTypes() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => !IsNonStandardInfrastructureController(method.DeclaringType))
            .Where(method => !method.GetCustomAttributes<ProducesResponseTypeAttribute>().Any())
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_UseStandardApiErrorContract_ForExplicitErrorResponses() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(attribute => attribute.StatusCode >= 400)
                .Where(attribute => attribute.Type != typeof(ApiErrorHttpResponse))
                .Select(_ => FormatMethodName(method)))
            .Distinct(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void AuthFeatureControllers_UseExpectedRoutePrefixes() {
        var expectedRoutes = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["AdminSsoController"] = "api/v{version:apiVersion}/auth/admin-sso",
            ["AuthPasswordController"] = "api/v{version:apiVersion}/auth/password-reset",
            ["AuthSessionController"] = "api/v{version:apiVersion}/auth",
            ["AuthTelegramController"] = "api/v{version:apiVersion}/auth/telegram",
        };

        Type[] authControllers = [.. GetFeatureControllerTypes()
            .Where(type => string.Equals(type.Namespace, "FoodDiary.Presentation.Api.Features.Auth", StringComparison.Ordinal))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];

        Dictionary<string, string> actualRoutes = authControllers.ToDictionary(
            type => type.Name,
            type => type.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty,
            StringComparer.Ordinal);

        Assert.Equal(expectedRoutes, actualRoutes);
    }

    [Fact]
    public void FeatureControllerBodyParameters_UsePresentationHttpRequestTypes() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
                .Where(parameter => !IsPresentationHttpRequestType(parameter.ParameterType))
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerHttpRequestParameters_ExplicitlyUseFromBody() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => IsPresentationHttpRequestType(parameter.ParameterType))
                .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is null)
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerComplexQueryParameters_UsePresentationHttpQueryTypes() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
                .Where(parameter => !IsSimpleTransportScalar(parameter.ParameterType))
                .Where(parameter => !IsPresentationHttpQueryType(parameter.ParameterType))
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))];

        Assert.Empty(violations);
    }

    [Fact]
    public void NonAuthFeatureControllers_RequireAuthorizationAtControllerLevel() {
        string?[] violations = [.. GetFeatureControllerTypes()
            .Where(type => !string.Equals(type.Namespace, "FoodDiary.Presentation.Api.Features.Auth", StringComparison.Ordinal))
            .Where(type => !IsAnonymousInfrastructureController(type))
            .Where(type => !type.IsAssignableTo(typeof(FoodDiary.Presentation.Api.Controllers.AuthorizedController)))
            .Where(type => type.GetCustomAttribute<AuthorizeAttribute>() is null)
            .Select(type => type.FullName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void NonAuthFeatureActions_DoNotDocumentUnauthorizedOrForbiddenResponses_Manually() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => !string.Equals(method.DeclaringType?.Namespace, "FoodDiary.Presentation.Api.Features.Auth", StringComparison.Ordinal))
            .Where(DeclaresProtectedResponses)
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void AllowAnonymous_IsUsedOnlyInAuthFeature() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => !IsAnonymousInfrastructureController(method.DeclaringType))
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Where(method => !string.Equals(method.DeclaringType?.Namespace, "FoodDiary.Presentation.Api.Features.Auth", StringComparison.Ordinal))
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void AllowAnonymous_UsageMatchesReviewedAllowlist() {
        string[] expected = [
            "AdminSsoController.AdminSsoExchange",
            "BillingWebhookController",
            "LogsController",
            "MarketingAttributionController",
        ];
        string[] actual = [.. GetControllerSyntaxTrees()
            .SelectMany(static tree => tree.GetRoot().DescendantNodes()
                .Where(static node => node is ClassDeclarationSyntax or MethodDeclarationSyntax)
                .Where(static node => node.ChildNodes().OfType<AttributeListSyntax>()
                    .SelectMany(static list => list.Attributes)
                    .Any(static attribute => string.Equals(attribute.Name.ToString(), "AllowAnonymous", StringComparison.Ordinal)))
                .Select(static node => node switch {
                    ClassDeclarationSyntax declaration => declaration.Identifier.ValueText,
                    MethodDeclarationSyntax method => $"{method.FirstAncestorOrSelf<ClassDeclarationSyntax>()!.Identifier.ValueText}.{method.Identifier.ValueText}",
                    _ => throw new UnreachableException(),
                }))
            .Order(StringComparer.Ordinal)];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SimpleFeatureControllers_UseBaseControllerHelpers_InsteadOfDirectMediatorSend() {
        string[] violations = [.. GetControllerSyntaxTrees()
            .Where(tree => tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(static invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "Send" }))
            .Select(static tree => Path.GetFileName(tree.FilePath))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_AreExpressionBodied() {
        string[] violations = [.. GetControllerSyntaxTrees()
            .SelectMany(static tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(static method => method.AttributeLists
                .SelectMany(static list => list.Attributes)
                .Any(static attribute => attribute.Name.ToString().StartsWith("Http", StringComparison.Ordinal)))
            .Where(static method => method.ExpressionBody is null)
            .Select(static method => method.Identifier.ValueText)
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotUseControllerTokenInRoutes() {
        string?[] violations = [.. GetFeatureControllerTypes()
            .Where(type => type.GetCustomAttribute<RouteAttribute>()?.Template?.Contains("[controller]", StringComparison.OrdinalIgnoreCase) is true)
            .Select(type => type.FullName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void HttpGetActions_DoNotDeclareCreatedResponses() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<HttpGetAttribute>().Any())
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Any(attribute => attribute.StatusCode == StatusCodes.Status201Created))
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void HandleCreatedActions_DeclareCreatedResponses() {
        string[] violations = [.. GetControllerSyntaxTrees()
            .SelectMany(tree => GetHandleCreatedMethods(tree)
                .Select(methodName => (tree, methodName)))
            .Where(tuple => {
                Type? controllerType = PresentationAssembly.GetTypes()
                    .FirstOrDefault(type => string.Equals(Path.GetFileName(tuple.tree.FilePath), $"{type.Name}.cs", StringComparison.Ordinal));

                if (controllerType is null) {
                    return true;
                }

                MethodInfo? method = GetActionMethods(controllerType)
                    .SingleOrDefault(candidate => string.Equals(candidate.Name, tuple.methodName, StringComparison.Ordinal));

                return method?.GetCustomAttributes<ProducesResponseTypeAttribute>()
                    .All(attribute => attribute.StatusCode != StatusCodes.Status201Created) != false;
            })
            .Select(tuple => $"{Path.GetFileNameWithoutExtension(tuple.tree.FilePath)}.{tuple.methodName}")];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_DoNotDocumentInternalServerError_Manually() {
        string[] violations = [.. GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Any(attribute => attribute.StatusCode == StatusCodes.Status500InternalServerError))
            .Select(FormatMethodName)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotReferenceApplicationTypesDirectly() {
        string[] violations = [.. GetControllerSyntaxTrees()
            .Where(static tree => !IsAllowedApplicationAbstractionReference(tree.FilePath))
            .Where(static tree => ReferencesApplicationTypes(tree))
            .Select(static tree => Path.GetFileName(tree.FilePath))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("*HttpRequest.cs", "Requests")]
    [InlineData("*HttpQuery.cs", "Requests")]
    [InlineData("*HttpResponse.cs", "Responses")]
    [InlineData("*HttpMappings.cs", "Mappings")]
    [InlineData("*HttpQueryMappings.cs", "Mappings")]
    [InlineData("*HttpResponseMappings.cs", "Mappings")]
    public void FeatureTransportFiles_LiveInExpectedFolders(string filePattern, string expectedFolderName) {
        string presentationRoot = GetPresentationRoot();
        string[] violations = [.. Directory.GetFiles(Path.Combine(presentationRoot, "Features"), filePattern, SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), expectedFolderName, StringComparison.Ordinal))
            .Select(static path => Path.GetRelativePath(GetPresentationRoot(), path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureTransportFiles_DoNotLiveOutsideFeaturesFolder() {
        string presentationRoot = GetPresentationRoot();
        string[] patterns = [
            "*HttpRequest.cs",
            "*HttpQuery.cs",
            "*HttpResponse.cs",
            "*HttpMappings.cs",
            "*HttpQueryMappings.cs",
            "*HttpResponseMappings.cs",
        ];

        string[] violations = [.. patterns
            .SelectMany(pattern => Directory.GetFiles(presentationRoot, pattern, SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetFileName(path) is not nameof(ApiErrorHttpResponse) + ".cs"
                and not "PagedHttpResponse.cs"
                and not "PagedHttpResponseMappings.cs"
                and not "EnumerableHttpResponseMappings.cs")
            .Select(path => Path.GetRelativePath(presentationRoot, path))
            .Order(StringComparer.Ordinal)];

        Assert.Empty(violations);
    }

    private static IEnumerable<SyntaxTree> GetControllerSyntaxTrees() {
        string presentationRoot = GetPresentationRoot();
        return Directory.GetFiles(Path.Combine(presentationRoot, "Features"), "*Controller.cs", SearchOption.AllDirectories)
            .Select(static path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path));
    }

    private static string GetPresentationRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "FoodDiary.Presentation.Api"));

    private static bool ReferencesApplicationTypes(SyntaxTree tree) {
        SyntaxNode root = tree.GetRoot();

        if (root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(static directive => directive.Name?.ToString().StartsWith("FoodDiary.Application", StringComparison.Ordinal) is true)) {
            return true;
        }

        return root.DescendantNodes()
            .OfType<QualifiedNameSyntax>()
            .Any(static name => name.ToString().StartsWith("FoodDiary.Application.", StringComparison.Ordinal));
    }

    private static bool IsAllowedApplicationAbstractionReference(string filePath) =>
        Path.GetFileName(filePath) is "NotificationsController.cs" or "NotificationPushController.cs";

    private static IEnumerable<string> GetHandleCreatedMethods(SyntaxTree tree) =>
        tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(static method => method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "HandleCreated" }))
            .Select(static method => method.Identifier.ValueText);

    private static Type[] GetFeatureControllerTypes() =>
        [.. PresentationAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true })
            .Where(type => type.Namespace?.StartsWith("FoodDiary.Presentation.Api.Features.", StringComparison.Ordinal) is true)
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal))];

    private static MethodInfo[] GetActionMethods(Type controllerType) =>
        [.. controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())];

    private static bool DeclaresProtectedResponses(MethodInfo method) =>
        method.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Any(attribute => attribute.StatusCode is 401 or 403);

    private static bool IsPresentationHttpRequestType(Type type) =>
        type.Assembly == PresentationAssembly &&
        type.Name.EndsWith("HttpRequest", StringComparison.Ordinal);

    private static bool IsPresentationHttpQueryType(Type type) =>
        type.Assembly == PresentationAssembly &&
        type.Name.EndsWith("HttpQuery", StringComparison.Ordinal);

    private static bool IsSimpleTransportScalar(Type type) {
        Type actualType = Nullable.GetUnderlyingType(type) ?? type;
        return actualType.IsPrimitive ||
               actualType.IsEnum ||
               actualType == typeof(string) ||
               actualType == typeof(decimal) ||
               actualType == typeof(Guid) ||
               actualType == typeof(DateTime) ||
               actualType == typeof(DateTimeOffset) ||
               actualType == typeof(DateOnly) ||
               actualType == typeof(TimeOnly);
    }

    private static bool IsAnonymousInfrastructureController(Type? type) =>
        type?.FullName is
            "FoodDiary.Presentation.Api.Features.Billing.BillingWebhookController" or
            "FoodDiary.Presentation.Api.Features.Logs.LogsController" or
            "FoodDiary.Presentation.Api.Features.Marketing.MarketingAttributionController" or
            "FoodDiary.Presentation.Api.Features.Version.VersionController";

    private static bool IsNonStandardInfrastructureController(Type? type) =>
        type?.FullName is "FoodDiary.Presentation.Api.Features.Version.VersionController";

    private static string FormatMethodName(MethodInfo method) =>
        $"{method.DeclaringType!.FullName}.{method.Name}";
}
