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
    public void ActionsDocumentingUnauthorizedOrForbiddenResponses_AreProtected() {
        var violations = GetFeatureControllerTypes()
            .SelectMany(GetActionMethods)
            .Where(method => method.DeclaringType?.Namespace != "FoodDiary.Presentation.Api.Features.Auth")
            .Where(DeclaresProtectedResponses)
            .Where(method => !IsProtectedAction(method))
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

    private static bool IsProtectedAction(MethodInfo method) {
        if (method.GetCustomAttributes<AllowAnonymousAttribute>().Any()) {
            return false;
        }

        if (method.GetCustomAttributes<AuthorizeAttribute>().Any()) {
            return true;
        }

        return method.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>().Any() is true;
    }

    private static string FormatMethodName(MethodInfo method) =>
        $"{method.DeclaringType!.FullName}.{method.Name}";
}
