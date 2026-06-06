using System.Reflection;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Consumptions;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Images;
using FoodDiary.Presentation.Api.Features.Products;
using FoodDiary.Presentation.Api.Features.Recipes;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ControllerSecurityContractTests {
    [Fact]
    public void AiFoodController_RequiresPremiumRole_AndAiRateLimitPolicy() {
        AuthorizeAttribute[] authorizeAttributes = [.. typeof(AiFoodController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)];
        EnableRateLimitingAttribute rateLimit = AssertSingleAttribute<EnableRateLimitingAttribute>(typeof(AiFoodController));

        Assert.NotEmpty(authorizeAttributes);
        Assert.Contains(authorizeAttributes, static attribute => string.Equals(attribute.Roles, PresentationRoleNames.Premium, StringComparison.Ordinal));
        Assert.Equal(PresentationPolicyNames.AiRateLimitPolicyName, rateLimit.PolicyName);
    }

    [Fact]
    public void AiFoodController_Actions_RequireCurrentUserBinding() {
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.AnalyzeFood));
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.CalculateNutrition));
    }

    [Fact]
    public void AuthController_SensitiveActions_UseAuthRateLimitPolicy() {
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.Register), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.Login), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.Refresh), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.RestoreAccount), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.VerifyEmail), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthSessionController), nameof(AuthSessionController.ResendVerifyEmail), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AdminSsoController), nameof(AdminSsoController.AdminSsoExchange), PresentationPolicyNames.AuthRateLimitPolicyName);
        AssertActionRateLimit(typeof(AuthTelegramController), nameof(AuthTelegramController.TelegramBotAuth), PresentationPolicyNames.AuthRateLimitPolicyName);
    }

    [Fact]
    public void AuthController_TelegramBotAuth_RequiresTelegramBotSecret() {
        MethodInfo method = GetAction(typeof(AuthTelegramController), nameof(AuthTelegramController.TelegramBotAuth));

        Assert.NotNull(method.GetCustomAttribute<RequireTelegramBotSecretAttribute>());
    }

    [Fact]
    public void AuthController_AdminSsoStart_RequiresAdminRole() {
        MethodInfo method = GetAction(typeof(AdminSsoController), nameof(AdminSsoController.AdminSsoStart));
        AuthorizeAttribute authorize = AssertSingleAttribute<AuthorizeAttribute>(method);

        Assert.Equal(PresentationRoleNames.Admin, authorize.Roles);
    }

    [Fact]
    public void AuthController_AdminSsoExchange_AllowsAnonymous() {
        MethodInfo method = GetAction(typeof(AdminSsoController), nameof(AdminSsoController.AdminSsoExchange));

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void ImagesController_Actions_RequireCurrentUserBinding() {
        AssertHasFromCurrentUserParameter(typeof(ImagesController), nameof(ImagesController.GetUploadUrl));
        AssertHasFromCurrentUserParameter(typeof(ImagesController), nameof(ImagesController.Delete));
    }

    [Fact]
    public void ImagesController_GetUploadUrl_UsesAuthRateLimitPolicy() {
        AssertActionRateLimit(typeof(ImagesController), nameof(ImagesController.GetUploadUrl), PresentationPolicyNames.AuthRateLimitPolicyName);
    }

    [Fact]
    public void AdminLessonsController_RequiresAdminRole() {
        AuthorizeAttribute[] authorizeAttributes = [.. typeof(AdminLessonsController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)];

        Assert.NotEmpty(authorizeAttributes);
        Assert.Contains(authorizeAttributes, static attribute => string.Equals(attribute.Roles, PresentationRoleNames.Admin, StringComparison.Ordinal));
    }

    [Fact]
    public void CriticalWriteActions_OptIntoExplicitIdempotencyPolicy() {
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(AuthSessionController), nameof(AuthSessionController.Refresh));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ProductsController), nameof(ProductsController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ProductsController), nameof(ProductsController.Duplicate));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(RecipesController), nameof(RecipesController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(RecipesController), nameof(RecipesController.Duplicate));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ConsumptionsController), nameof(ConsumptionsController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ImagesController), nameof(ImagesController.GetUploadUrl));
    }

    private static void AssertActionRateLimit(Type controllerType, string actionName, string expectedPolicyName) {
        MethodInfo method = GetAction(controllerType, actionName);
        EnableRateLimitingAttribute attribute = AssertSingleAttribute<EnableRateLimitingAttribute>(method);

        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }

    private static void AssertHasFromCurrentUserParameter(Type controllerType, string actionName) {
        MethodInfo method = GetAction(controllerType, actionName);
        ParameterInfo[] parameters = method.GetParameters();

        Assert.Contains(parameters, static parameter => parameter.GetCustomAttribute<FromCurrentUserAttribute>() is not null);
    }

    private static void AssertHasAttribute<TAttribute>(Type controllerType, string actionName)
        where TAttribute : Attribute {
        MethodInfo method = GetAction(controllerType, actionName);
        Assert.NotNull(method.GetCustomAttribute<TAttribute>());
    }

    private static MethodInfo GetAction(Type controllerType, string actionName) =>
        controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"Action {controllerType.FullName}.{actionName} was not found.");

    private static TAttribute AssertSingleAttribute<TAttribute>(Type type)
        where TAttribute : Attribute {
        TAttribute[] attributes = [.. type.GetCustomAttributes<TAttribute>(inherit: true)];
        Assert.Single(attributes);
        return attributes[0];
    }

    private static TAttribute AssertSingleAttribute<TAttribute>(MemberInfo member)
        where TAttribute : Attribute {
        TAttribute[] attributes = [.. member.GetCustomAttributes<TAttribute>(inherit: true)];
        Assert.Single(attributes);
        return attributes[0];
    }
}
