using System.Reflection;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Images;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ControllerSecurityContractTests {
    [Fact]
    public void AiFoodController_RequiresPremiumRole_AndAiRateLimitPolicy() {
        var authorizeAttributes = typeof(AiFoodController).GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();
        var rateLimit = AssertSingleAttribute<EnableRateLimitingAttribute>(typeof(AiFoodController));

        Assert.NotEmpty(authorizeAttributes);
        Assert.Contains(authorizeAttributes, static attribute => attribute.Roles == PresentationRoleNames.Premium);
        Assert.Equal(PresentationPolicyNames.AiRateLimitPolicyName, rateLimit.PolicyName);
    }

    [Fact]
    public void AiFoodController_Actions_RequireCurrentUserBinding() {
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.AnalyzeFood));
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.CalculateNutrition));
    }

    [Fact]
    public void AuthController_LoginAndRefresh_UseAuthRateLimitPolicy() {
        AssertActionRateLimit(nameof(AuthController.Login), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(nameof(AuthController.Refresh), PresentationPolicyNames.AuthRateLimitPolicyName);
    }

    [Fact]
    public void AuthController_TelegramBotAuth_RequiresTelegramBotSecret() {
        var method = GetAction(typeof(AuthController), nameof(AuthController.TelegramBotAuth));

        Assert.NotNull(method.GetCustomAttribute<RequireTelegramBotSecretAttribute>());
    }

    [Fact]
    public void AuthController_AdminSsoStart_RequiresAdminRole() {
        var method = GetAction(typeof(AuthController), nameof(AuthController.AdminSsoStart));
        var authorize = AssertSingleAttribute<AuthorizeAttribute>(method);

        Assert.Equal(PresentationRoleNames.Admin, authorize.Roles);
    }

    [Fact]
    public void AuthController_AdminSsoExchange_AllowsAnonymous() {
        var method = GetAction(typeof(AuthController), nameof(AuthController.AdminSsoExchange));

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void ImagesController_Actions_RequireCurrentUserBinding() {
        AssertHasFromCurrentUserParameter(typeof(ImagesController), nameof(ImagesController.GetUploadUrl));
        AssertHasFromCurrentUserParameter(typeof(ImagesController), nameof(ImagesController.Delete));
    }

    private static void AssertActionRateLimit(string actionName, string expectedPolicyName) {
        var method = GetAction(typeof(AuthController), actionName);
        var attribute = AssertSingleAttribute<EnableRateLimitingAttribute>(method);

        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }

    private static void AssertHasFromCurrentUserParameter(Type controllerType, string actionName) {
        var method = GetAction(controllerType, actionName);
        var parameters = method.GetParameters();

        Assert.Contains(parameters, static parameter => parameter.GetCustomAttribute<FromCurrentUserAttribute>() is not null);
    }

    private static MethodInfo GetAction(Type controllerType, string actionName) =>
        controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"Action {controllerType.FullName}.{actionName} was not found.");

    private static TAttribute AssertSingleAttribute<TAttribute>(Type type)
        where TAttribute : Attribute {
        var attributes = type.GetCustomAttributes<TAttribute>(inherit: true).ToArray();
        Assert.Single(attributes);
        return attributes[0];
    }

    private static TAttribute AssertSingleAttribute<TAttribute>(MemberInfo member)
        where TAttribute : Attribute {
        var attributes = member.GetCustomAttributes<TAttribute>(inherit: true).ToArray();
        Assert.Single(attributes);
        return attributes[0];
    }
}
