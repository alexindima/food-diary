using System.Reflection;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ControllerConventionsTests {
    private static readonly Assembly PresentationAssembly = typeof(FoodDiary.Presentation.Api.Controllers.BaseApiController).Assembly;

    [Fact]
    public void FeatureControllers_HaveApiControllerAttribute() {
        var violations = GetFeatureControllerTypes()
            .Where(type => type.GetCustomAttribute<ApiControllerAttribute>() is null)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_ReturnTaskOfActionResult() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.ReturnType != typeof(Task<IActionResult>))
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotExposeMoreThanEightActions() {
        var violations = GetFeatureControllerTypes()
            .Select(type => new {
                Type = type,
                ActionCount = GetActionMethods(type).Length,
            })
            .Where(static entry => entry.ActionCount > 8)
            .Select(entry => $"{entry.Type.FullName} ({entry.ActionCount} actions)")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_DeclareProducesResponseTypes() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>().Any() is false)
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_UseStandardApiErrorContract_ForExplicitErrorResponses() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(attribute => attribute.StatusCode >= 400)
                .Where(attribute => attribute.Type != typeof(ApiErrorHttpResponse))
                .Select(_ => FormatMethodName(method)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

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

        var authControllers = GetFeatureControllerTypes()
            .Where(type => type.Namespace == "FoodDiary.Presentation.Api.Features.Auth")
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

        var actualRoutes = authControllers.ToDictionary(
            type => type.Name,
            type => type.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty,
            StringComparer.Ordinal);

        Assert.Equal(expectedRoutes, actualRoutes);
    }

    [Fact]
    public void FeatureControllerBodyParameters_UsePresentationHttpRequestTypes() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
                .Where(parameter => !IsPresentationHttpRequestType(parameter.ParameterType))
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerHttpRequestParameters_ExplicitlyUseFromBody() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => IsPresentationHttpRequestType(parameter.ParameterType))
                .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is null)
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerComplexQueryParameters_UsePresentationHttpQueryTypes() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .SelectMany(method => method.GetParameters()
                .Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null)
                .Where(parameter => !IsSimpleTransportScalar(parameter.ParameterType))
                .Where(parameter => !IsPresentationHttpQueryType(parameter.ParameterType))
                .Select(parameter => $"{FormatMethodName(method)} parameter {parameter.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void NonAuthFeatureControllers_RequireAuthorizationAtControllerLevel() {
        var violations = GetFeatureControllerTypes()
            .Where(type => type.Namespace != "FoodDiary.Presentation.Api.Features.Auth")
            .Where(type => type.IsAssignableTo(typeof(FoodDiary.Presentation.Api.Controllers.AuthorizedController)) is false)
            .Where(type => type.GetCustomAttribute<AuthorizeAttribute>() is null)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void NonAuthFeatureActions_DoNotDocumentUnauthorizedOrForbiddenResponses_Manually() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.DeclaringType?.Namespace != "FoodDiary.Presentation.Api.Features.Auth")
            .Where(DeclaresProtectedResponses)
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void AllowAnonymous_IsUsedOnlyInAuthFeature() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Where(method => method.DeclaringType?.Namespace != "FoodDiary.Presentation.Api.Features.Auth")
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void SimpleFeatureControllers_UseBaseControllerHelpers_InsteadOfDirectMediatorSend() {
        var violations = GetControllerSyntaxTrees()
            .Where(tree => tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(static invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "Send" }))
            .Select(static tree => Path.GetFileName(tree.FilePath))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotUseControllerTokenInRoutes() {
        var violations = GetFeatureControllerTypes()
            .Where(type => type.GetCustomAttribute<RouteAttribute>()?.Template?.Contains("[controller]", StringComparison.OrdinalIgnoreCase) is true)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void HttpGetActions_DoNotDeclareCreatedResponses() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<HttpGetAttribute>().Any())
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Any(attribute => attribute.StatusCode == StatusCodes.Status201Created))
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void HandleCreatedActions_DeclareCreatedResponses() {
        var violations = GetControllerSyntaxTrees()
            .SelectMany(tree => GetHandleCreatedMethods(tree)
                .Select(methodName => (tree, methodName)))
            .Where(tuple => {
                var controllerType = PresentationAssembly.GetTypes()
                    .FirstOrDefault(type => string.Equals(Path.GetFileName(tuple.tree.FilePath), $"{type.Name}.cs", StringComparison.Ordinal));

                if (controllerType is null) {
                    return true;
                }

                var method = GetActionMethods(controllerType)
                    .SingleOrDefault(candidate => string.Equals(candidate.Name, tuple.methodName, StringComparison.Ordinal));

                return method is null || method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                    .All(attribute => attribute.StatusCode != StatusCodes.Status201Created);
            })
            .Select(tuple => $"{Path.GetFileNameWithoutExtension(tuple.tree.FilePath)}.{tuple.methodName}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_DoNotDocumentInternalServerError_Manually() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Any(attribute => attribute.StatusCode == StatusCodes.Status500InternalServerError))
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllers_DoNotReferenceApplicationTypesDirectly() {
        var violations = GetControllerSyntaxTrees()
            .Where(static tree => ReferencesApplicationTypes(tree))
            .Select(static tree => Path.GetFileName(tree.FilePath))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

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
        var presentationRoot = GetPresentationRoot();
        var violations = Directory.GetFiles(Path.Combine(presentationRoot, "Features"), filePattern, SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), expectedFolderName, StringComparison.Ordinal) is false)
            .Select(static path => Path.GetRelativePath(GetPresentationRoot(), path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureTransportFiles_DoNotLiveOutsideFeaturesFolder() {
        var presentationRoot = GetPresentationRoot();
        var patterns = new[] {
            "*HttpRequest.cs",
            "*HttpQuery.cs",
            "*HttpResponse.cs",
            "*HttpMappings.cs",
            "*HttpQueryMappings.cs",
            "*HttpResponseMappings.cs",
        };

        var violations = patterns
            .SelectMany(pattern => Directory.GetFiles(presentationRoot, pattern, SearchOption.AllDirectories))
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false)
            .Where(path => Path.GetFileName(path) is not nameof(ApiErrorHttpResponse) + ".cs"
                and not "PagedHttpResponse.cs"
                and not "PagedHttpResponseMappings.cs"
                and not "EnumerableHttpResponseMappings.cs")
            .Select(path => Path.GetRelativePath(presentationRoot, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<SyntaxTree> GetControllerSyntaxTrees() {
        var presentationRoot = GetPresentationRoot();
        return Directory.GetFiles(Path.Combine(presentationRoot, "Features"), "*Controller.cs", SearchOption.AllDirectories)
            .Select(static path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path));
    }

    private static string GetPresentationRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "FoodDiary.Presentation.Api"));

    private static bool ReferencesApplicationTypes(SyntaxTree tree) {
        var root = tree.GetRoot();

        if (root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(static directive => directive.Name?.ToString().StartsWith("FoodDiary.Application", StringComparison.Ordinal) is true)) {
            return true;
        }

        return root.DescendantNodes()
            .OfType<QualifiedNameSyntax>()
            .Any(static name => name.ToString().StartsWith("FoodDiary.Application.", StringComparison.Ordinal));
    }

    private static IEnumerable<string> GetHandleCreatedMethods(SyntaxTree tree) =>
        tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(static method => method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "HandleCreated" }))
            .Select(static method => method.Identifier.ValueText);

    private static Type[] GetFeatureControllerTypes() =>
        PresentationAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true })
            .Where(type => type.Namespace?.StartsWith("FoodDiary.Presentation.Api.Features.", StringComparison.Ordinal) is true)
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToArray();

    private static MethodInfo[] GetActionMethods(Type controllerType) =>
        controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

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
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
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

    private static string FormatMethodName(MethodInfo method) =>
        $"{method.DeclaringType!.FullName}.{method.Name}";
}
