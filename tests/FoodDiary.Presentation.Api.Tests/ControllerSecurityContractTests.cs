using System.Reflection;
using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Features.Meals;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Billing;
using FoodDiary.Presentation.Api.Features.Dashboard;
using FoodDiary.Presentation.Api.Features.Images;
using FoodDiary.Presentation.Api.Features.Export;
using FoodDiary.Presentation.Api.Features.Logs;
using FoodDiary.Presentation.Api.Features.Marketing;
using FoodDiary.Presentation.Api.Features.MealPlans;
using FoodDiary.Presentation.Api.Features.Notifications;
using FoodDiary.Presentation.Api.Features.Products;
using FoodDiary.Presentation.Api.Features.Recipes;
using FoodDiary.Presentation.Api.Features.Version;
using FoodDiary.Presentation.Api.Features.Wearables;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ControllerSecurityContractTests {
    [Fact]
    public void FromCurrentUserAttribute_UsesCustomBindingSource() {
        var attribute = new FromCurrentUserAttribute();

        Assert.Same(BindingSource.Custom, attribute.BindingSource);
    }

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
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.ParseFoodText));
        AssertHasFromCurrentUserParameter(typeof(AiFoodController), nameof(AiFoodController.CalculateNutrition));
    }

    [Fact]
    public void AiFoodController_WriteActions_RequireIdempotencyKey() {
        AssertRequiresIdempotencyKey(nameof(AiFoodController.AnalyzeFood));
        AssertRequiresIdempotencyKey(nameof(AiFoodController.ParseFoodText));
        AssertRequiresIdempotencyKey(nameof(AiFoodController.CalculateNutrition));
    }

    [Fact]
    public void AbuseProneJsonEndpoints_UseDedicatedRequestLimits() {
        AssertControllerRequestLimits(typeof(AiFoodController), PresentationRequestLimits.AiPayloadBytes);
        AssertActionRequestLimits(
            typeof(BulkRecommendationsController),
            nameof(BulkRecommendationsController.Create),
            PresentationRequestLimits.BulkRecommendationsPayloadBytes);
        AssertActionRequestLimits(
            typeof(AdminLessonsController),
            nameof(AdminLessonsController.Import),
            PresentationRequestLimits.AdminImportPayloadBytes);
        AssertActionRequestLimits(
            typeof(MealsController),
            nameof(MealsController.Create),
            PresentationRequestLimits.RichWritePayloadBytes);
        AssertActionRequestLimits(
            typeof(MealsController),
            nameof(MealsController.Update),
            PresentationRequestLimits.RichWritePayloadBytes);
        AssertActionRequestLimits(
            typeof(RecipesController),
            nameof(RecipesController.Create),
            PresentationRequestLimits.RichWritePayloadBytes);
        AssertActionRequestLimits(
            typeof(RecipesController),
            nameof(RecipesController.Update),
            PresentationRequestLimits.RichWritePayloadBytes);
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
    public void PresentationActions_HaveExplicitAuthorizationClassification() {
        string[] unclassifiedActions = [.. typeof(BaseApiController).Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(static type => type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(static method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Select(method => (Controller: type, Action: method)))
            .Where(static item =>
                !item.Controller.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any() &&
                !item.Controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any() &&
                !item.Action.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().Any() &&
                !item.Action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any())
            .Select(static item => $"{item.Controller.FullName}.{item.Action.Name}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(unclassifiedActions);
    }

    [Fact]
    public void AuthenticationPayloadEndpoints_UseDedicatedRequestLimits() {
        AssertControllerRequestLimits(typeof(AuthSessionController), AuthRequestLimits.MaxPayloadBytes);
        AssertControllerRequestLimits(typeof(AuthPasswordController), AuthRequestLimits.MaxPayloadBytes);
        AssertControllerRequestLimits(typeof(AuthTelegramController), AuthRequestLimits.MaxPayloadBytes);
        AssertActionRequestLimits(
            typeof(AdminSsoController),
            nameof(AdminSsoController.AdminSsoExchange),
            AuthRequestLimits.MaxPayloadBytes);
    }

    [Fact]
    public void AuthenticationRequestStrings_HaveExplicitTransportLengthConstraints() {
        Type[] requestTypes = [
            typeof(AdminSsoExchangeHttpRequest),
            typeof(ConfirmPasswordResetHttpRequest),
            typeof(GoogleLoginHttpRequest),
            typeof(LoginHttpRequest),
            typeof(RefreshTokenHttpRequest),
            typeof(RegisterHttpRequest),
            typeof(RequestPasswordResetHttpRequest),
            typeof(ResendEmailVerificationHttpRequest),
            typeof(RestoreAccountHttpRequest),
            typeof(TelegramAuthHttpRequest),
            typeof(TelegramLoginWidgetHttpRequest),
            typeof(VerifyEmailHttpRequest),
        ];
        var nullability = new NullabilityInfoContext();

        foreach (ParameterInfo parameter in requestTypes
            .SelectMany(static type => Assert.Single(type.GetConstructors()).GetParameters())
            .Where(static parameter => parameter.ParameterType == typeof(string))) {
            Assert.NotNull(parameter.GetCustomAttribute<MaxLengthAttribute>());
            if (nullability.Create(parameter).ReadState == NullabilityState.NotNull) {
                Assert.NotNull(parameter.GetCustomAttribute<RequiredAttribute>());
            }
        }

        Assert.NotNull(Assert.Single(typeof(TelegramBotAuthHttpRequest).GetConstructors())
            .GetParameters()
            .Single(static parameter => string.Equals(parameter.Name, "TelegramUserId", StringComparison.Ordinal))
            .GetCustomAttribute<RangeAttribute>());
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
    public void TestDeliveryActions_UseDedicatedRateLimitPolicy() {
        AssertActionRateLimit(
            typeof(DashboardController),
            nameof(DashboardController.SendTestEmail),
            PresentationPolicyNames.TestDeliveryRateLimitPolicyName);
        AssertActionRateLimit(
            typeof(NotificationsController),
            nameof(NotificationsController.ScheduleTestNotification),
            PresentationPolicyNames.TestDeliveryRateLimitPolicyName);
    }

    [Fact]
    public void AnonymousIngestionControllers_UseDedicatedRateLimitsAndRequestSizeLimits() {
        AssertControllerRateLimit(typeof(LogsController), PresentationPolicyNames.ClientTelemetryRateLimitPolicyName);
        AssertControllerRateLimit(typeof(MarketingAttributionController), PresentationPolicyNames.MarketingAttributionRateLimitPolicyName);
        AssertActionRequestSizeLimit(typeof(LogsController), nameof(LogsController.Create), LogsController.MaxPayloadBytes);
        AssertActionContentLengthLimit(typeof(LogsController), nameof(LogsController.Create), LogsController.MaxPayloadBytes);
        AssertActionRequestSizeLimit(
            typeof(MarketingAttributionController),
            nameof(MarketingAttributionController.Create),
            MarketingAttributionController.MaxPayloadBytes);
        AssertActionContentLengthLimit(
            typeof(MarketingAttributionController),
            nameof(MarketingAttributionController.Create),
            MarketingAttributionController.MaxPayloadBytes);
        AssertActionRequestSizeLimit(
            typeof(MarketingAttributionController),
            nameof(MarketingAttributionController.CreateSignup),
            MarketingAttributionController.MaxPayloadBytes);
        AssertActionContentLengthLimit(
            typeof(MarketingAttributionController),
            nameof(MarketingAttributionController.CreateSignup),
            MarketingAttributionController.MaxPayloadBytes);
    }

    [Fact]
    public void MarketingSignupAttribution_RequiresAuthorizationAndCurrentUserBinding() {
        MethodInfo method = GetAction(typeof(MarketingAttributionController), nameof(MarketingAttributionController.CreateSignup));

        Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        AssertHasFromCurrentUserParameter(typeof(MarketingAttributionController), nameof(MarketingAttributionController.CreateSignup));
    }

    [Fact]
    public void AdminLessonsController_RequiresAdminRole() {
        AuthorizeAttribute[] authorizeAttributes = [.. typeof(AdminLessonsController).GetCustomAttributes<AuthorizeAttribute>(inherit: true)];

        Assert.NotEmpty(authorizeAttributes);
        Assert.Contains(authorizeAttributes, static attribute => string.Equals(attribute.Roles, PresentationRoleNames.Admin, StringComparison.Ordinal));
    }

    [Fact]
    public void CriticalWriteActions_OptIntoExplicitIdempotencyPolicy() {
        Assert.DoesNotContain(
            GetAction(typeof(AuthSessionController), nameof(AuthSessionController.Refresh)).GetCustomAttributes(inherit: true),
            static attribute => attribute is EnableIdempotencyAttribute);
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ProductsController), nameof(ProductsController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ProductsController), nameof(ProductsController.Duplicate));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(RecipesController), nameof(RecipesController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(RecipesController), nameof(RecipesController.Duplicate));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(MealsController), nameof(MealsController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(MealsController), nameof(MealsController.Repeat));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(ImagesController), nameof(ImagesController.GetUploadUrl));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(BillingController), nameof(BillingController.StartPremiumTrial));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(BillingController), nameof(BillingController.CreateCheckoutSession));
        AssertRequiresIdempotencyKey(typeof(BillingController), nameof(BillingController.CreateCheckoutSession));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(BillingController), nameof(BillingController.CreatePortalSession));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(BulkRecommendationsController), nameof(BulkRecommendationsController.Create));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(AdminEmailTemplatesController), nameof(AdminEmailTemplatesController.SendTest));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(NotificationsController), nameof(NotificationsController.ScheduleTestNotification));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(MealPlansController), nameof(MealPlansController.Adopt));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(MealPlansController), nameof(MealPlansController.GenerateShoppingList));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(DietologistClientsController), nameof(DietologistClientsController.CreateRecommendation));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(DashboardController), nameof(DashboardController.SendTestEmail));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(DietologistController), nameof(DietologistController.Invite));
        AssertHasAttribute<EnableIdempotencyAttribute>(typeof(WearablesController), nameof(WearablesController.Sync));
        Assert.DoesNotContain(
            GetAction(typeof(AdminUserCreationController), nameof(AdminUserCreationController.CreateUser)).GetCustomAttributes(inherit: true),
            static attribute => attribute is EnableIdempotencyAttribute);
    }

    [Fact]
    public void WearableProviderMutations_EnforceSensitiveAccessAndResourcePolicies() {
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(WearablesController), nameof(WearablesController.Connect));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(WearablesController), nameof(WearablesController.Disconnect));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(WearablesController), nameof(WearablesController.Sync));
        AssertActionRateLimit(
            typeof(WearablesController),
            nameof(WearablesController.Connect),
            PresentationPolicyNames.WearableRateLimitPolicyName);
        AssertActionRateLimit(
            typeof(WearablesController),
            nameof(WearablesController.Sync),
            PresentationPolicyNames.WearableRateLimitPolicyName);

        EnableIdempotencyAttribute idempotency = AssertSingleAttribute<EnableIdempotencyAttribute>(
            GetAction(typeof(WearablesController), nameof(WearablesController.Sync)));
        Assert.True(idempotency.RequireKey);
    }

    [Fact]
    public void NonSensitiveCreatedWriteActions_OptIntoExplicitIdempotencyPolicy() {
        MethodInfo sensitiveAdminUserCreation = GetAction(
            typeof(AdminUserCreationController),
            nameof(AdminUserCreationController.CreateUser));
        string[] missingActions = [.. typeof(BaseApiController).Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(static type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(static method => method.GetCustomAttributes<HttpPostAttribute>(inherit: true).Any())
            .Where(static method => method.GetCustomAttributes<ProducesResponseTypeAttribute>(inherit: true)
                .Any(static attribute => attribute.StatusCode == StatusCodes.Status201Created))
            .Where(method => method != sensitiveAdminUserCreation)
            .Where(static method => method.GetCustomAttribute<EnableIdempotencyAttribute>(inherit: true) is null)
            .Select(static method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .Order(StringComparer.Ordinal)];

        Assert.Empty(missingActions);
    }

    [Fact]
    public void SensitiveAndDeploymentMetadataControllers_DisableClientAndProxyCaching() {
        Type[] controllerTypes = [
            typeof(AuthSessionController),
            typeof(AuthPasswordController),
            typeof(AuthTelegramController),
            typeof(AdminSsoController),
            typeof(ExportController),
            typeof(VersionController),
        ];

        Assert.All(controllerTypes, static controllerType => {
            ResponseCacheAttribute attribute = AssertSingleAttribute<ResponseCacheAttribute>(controllerType);

            Assert.True(attribute.NoStore);
            Assert.Equal(ResponseCacheLocation.None, attribute.Location);
        });
    }

    private static void AssertActionRateLimit(Type controllerType, string actionName, string expectedPolicyName) {
        MethodInfo method = GetAction(controllerType, actionName);
        EnableRateLimitingAttribute attribute = AssertSingleAttribute<EnableRateLimitingAttribute>(method);

        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }

    private static void AssertRequiresIdempotencyKey(string actionName) {
        AssertRequiresIdempotencyKey(typeof(AiFoodController), actionName);
    }

    private static void AssertRequiresIdempotencyKey(Type controllerType, string actionName) {
        MethodInfo method = GetAction(controllerType, actionName);
        EnableIdempotencyAttribute attribute = AssertSingleAttribute<EnableIdempotencyAttribute>(method);

        Assert.True(attribute.RequireKey);
    }

    private static void AssertControllerRateLimit(Type controllerType, string expectedPolicyName) {
        EnableRateLimitingAttribute attribute = AssertSingleAttribute<EnableRateLimitingAttribute>(controllerType);

        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }

    private static void AssertActionRequestSizeLimit(Type controllerType, string actionName, long expectedBytes) {
        MethodInfo method = GetAction(controllerType, actionName);
        CustomAttributeData attribute = Assert.Single(
            method.CustomAttributes,
            static attribute => attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        CustomAttributeTypedArgument bytes = Assert.Single(attribute.ConstructorArguments);

        Assert.Equal(expectedBytes, bytes.Value);
    }

    private static void AssertActionContentLengthLimit(Type controllerType, string actionName, long expectedBytes) {
        MethodInfo method = GetAction(controllerType, actionName);
        RejectOversizedRequestAttribute attribute = AssertSingleAttribute<RejectOversizedRequestAttribute>(method);

        Assert.Equal(expectedBytes, attribute.MaxBytes);
    }

    private static void AssertControllerRequestLimits(Type controllerType, long expectedBytes) {
        AssertRequestLimits(controllerType, expectedBytes);
    }

    private static void AssertActionRequestLimits(Type controllerType, string actionName, long expectedBytes) {
        AssertRequestLimits(GetAction(controllerType, actionName), expectedBytes);
    }

    private static void AssertRequestLimits(MemberInfo member, long expectedBytes) {
        CustomAttributeData requestSizeLimit = Assert.Single(
            member.CustomAttributes,
            static attribute => attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        RejectOversizedRequestAttribute contentLengthLimit = AssertSingleAttribute<RejectOversizedRequestAttribute>(member);
        ProducesApiErrorResponseAttribute payloadTooLarge = Assert.Single(
            member.GetCustomAttributes<ProducesApiErrorResponseAttribute>(inherit: true),
            static attribute => attribute.StatusCode == StatusCodes.Status413PayloadTooLarge);

        Assert.Multiple(
            () => Assert.Equal(expectedBytes, Assert.Single(requestSizeLimit.ConstructorArguments).Value),
            () => Assert.Equal(expectedBytes, contentLengthLimit.MaxBytes),
            () => Assert.Equal(StatusCodes.Status413PayloadTooLarge, payloadTooLarge.StatusCode));
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
