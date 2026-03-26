using System.Reflection;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
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
    public void FeatureControllerActions_DeclareProducesResponseTypes() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>().Any() is false)
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureControllerActions_DeclareStandardApiErrorContract() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(attribute => attribute.StatusCode >= 400)
                .Any(attribute => attribute.Type == typeof(ApiErrorHttpResponse)) is false)
            .Select(FormatMethodName)
            .ToArray();

        Assert.Empty(violations);
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
        var presentationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "FoodDiary.Presentation.Api"));
        var violations = Directory.GetFiles(Path.Combine(presentationRoot, "Features"), "*Controller.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("await Send(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
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
    public void FeatureControllers_DoNotReferenceApplicationTypesDirectly() {
        var presentationRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "FoodDiary.Presentation.Api"));
        var violations = Directory.GetFiles(Path.Combine(presentationRoot, "Features"), "*Controller.cs", SearchOption.AllDirectories)
            .Where(static path => File.ReadAllText(path).Contains("FoodDiary.Application", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

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
