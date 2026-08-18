using System.Reflection;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Notifications;
using FoodDiary.Presentation.Api.Features.ShoppingLists;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class PresentationHardeningContractTests {
    [Fact]
    public void DurableIdentityAndNotificationChannelMutations_BlockImpersonatedAccess() {
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(AuthSessionController), nameof(AuthSessionController.LinkGoogle));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(NotificationPushController), nameof(NotificationPushController.UpsertWebPushSubscription));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(NotificationPushController), nameof(NotificationPushController.RemoveWebPushSubscription));
    }

    [Fact]
    public void ShoppingListWriteActions_UseRichWriteRequestLimits() {
        AssertRequestLimits(typeof(ShoppingListsController), nameof(ShoppingListsController.Create));
        AssertRequestLimits(typeof(ShoppingListsController), nameof(ShoppingListsController.Update));
    }

    private static void AssertRequestLimits(Type controllerType, string actionName) {
        MethodInfo action = GetAction(controllerType, actionName);
        CustomAttributeData requestSizeLimit = Assert.Single(
            action.CustomAttributes,
            static attribute => attribute.AttributeType == typeof(RequestSizeLimitAttribute));
        RejectOversizedRequestAttribute contentLengthLimit = Assert.Single(
            action.GetCustomAttributes<RejectOversizedRequestAttribute>(inherit: true));
        ProducesApiErrorResponseAttribute payloadTooLarge = Assert.Single(
            action.GetCustomAttributes<ProducesApiErrorResponseAttribute>(inherit: true),
            static attribute => attribute.StatusCode == StatusCodes.Status413PayloadTooLarge);

        Assert.Multiple(
            () => Assert.Equal(PresentationRequestLimits.RichWritePayloadBytes, Assert.Single(requestSizeLimit.ConstructorArguments).Value),
            () => Assert.Equal(PresentationRequestLimits.RichWritePayloadBytes, contentLengthLimit.MaxBytes),
            () => Assert.Equal(StatusCodes.Status413PayloadTooLarge, payloadTooLarge.StatusCode));
    }

    private static void AssertHasAttribute<TAttribute>(Type controllerType, string actionName)
        where TAttribute : Attribute =>
        Assert.NotNull(GetAction(controllerType, actionName).GetCustomAttribute<TAttribute>());

    private static MethodInfo GetAction(Type controllerType, string actionName) =>
        controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"Action {controllerType.FullName}.{actionName} was not found.");
}
