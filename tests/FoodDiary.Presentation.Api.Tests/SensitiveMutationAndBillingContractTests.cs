using System.Reflection;
using FoodDiary.Presentation.Api.Features.Billing;
using FoodDiary.Presentation.Api.Features.Cycles;
using FoodDiary.Presentation.Api.Features.Dietologist;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class SensitiveMutationAndBillingContractTests {
    [Fact]
    public void DietologistRelationshipMutations_BlockImpersonatedAccess() {
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistController), nameof(DietologistController.Invite));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistController), nameof(DietologistController.RevokeOrDisconnect));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistController), nameof(DietologistController.UpdatePermissions));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistInvitationsController), nameof(DietologistInvitationsController.Accept));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistInvitationsController), nameof(DietologistInvitationsController.Decline));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistInvitationsController), nameof(DietologistInvitationsController.AcceptForCurrentUser));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(DietologistInvitationsController), nameof(DietologistInvitationsController.DeclineForCurrentUser));
    }

    [Fact]
    public void CycleConsentMutations_BlockImpersonatedAccess() {
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(CyclesController), nameof(CyclesController.Create));
        AssertHasAttribute<BlockImpersonatedAccessAttribute>(typeof(CyclesController), nameof(CyclesController.UpdateConsent));
    }

    [Fact]
    public void BillingProviderOperations_DocumentExternalFailureResponse() {
        AssertDocumentsApiError(
            typeof(BillingController),
            nameof(BillingController.CreateCheckoutSession),
            StatusCodes.Status502BadGateway);
        AssertDocumentsApiError(
            typeof(BillingController),
            nameof(BillingController.CreatePortalSession),
            StatusCodes.Status502BadGateway);
        AssertDocumentsApiError(
            typeof(BillingWebhookController),
            nameof(BillingWebhookController.HandleWebhook),
            StatusCodes.Status502BadGateway);
    }

    private static void AssertHasAttribute<TAttribute>(Type controllerType, string actionName)
        where TAttribute : Attribute =>
        Assert.NotNull(GetAction(controllerType, actionName).GetCustomAttribute<TAttribute>());

    private static void AssertDocumentsApiError(Type controllerType, string actionName, int statusCode) =>
        Assert.Contains(
            GetAction(controllerType, actionName).GetCustomAttributes<ProducesApiErrorResponseAttribute>(inherit: true),
            attribute => attribute.StatusCode == statusCode);

    private static MethodInfo GetAction(Type controllerType, string actionName) =>
        controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        ?? throw new InvalidOperationException($"Action {controllerType.FullName}.{actionName} was not found.");
}
