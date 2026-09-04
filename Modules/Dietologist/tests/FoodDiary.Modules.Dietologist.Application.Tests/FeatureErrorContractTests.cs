using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void DietologistErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Dietologist.Application.Abstractions", typeof(DietologistErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Dietologist.Common", typeof(DietologistErrors).Namespace));
    }

    [Fact]
    public void DietologistErrors_PreservesEveryPublicErrorContract() {
        AssertError(DietologistErrors.InvitationNotFound, "Dietologist.InvitationNotFound", "Dietologist invitation was not found.", ErrorKind.NotFound);
        AssertError(DietologistErrors.InvitationExpired, "Dietologist.InvitationExpired", "Dietologist invitation has expired.", ErrorKind.Validation);
        AssertError(DietologistErrors.InvitationInvalidToken, "Dietologist.InvitationInvalidToken", "Invitation token is invalid.", ErrorKind.Unauthorized);
        AssertError(DietologistErrors.AlreadyHasDietologist, "Dietologist.AlreadyHasDietologist", "You already have an active dietologist.", ErrorKind.Conflict);
        AssertError(DietologistErrors.PendingInvitationExists, "Dietologist.PendingInvitationExists", "A pending invitation already exists.", ErrorKind.Conflict);
        AssertError(DietologistErrors.CannotInviteSelf, "Dietologist.CannotInviteSelf", "You cannot invite yourself as a dietologist.", ErrorKind.Validation);
        AssertError(DietologistErrors.AccessDenied, "Dietologist.AccessDenied", "You do not have access to this client's data.", ErrorKind.Forbidden);
        AssertError(DietologistErrors.PermissionDenied, "Dietologist.PermissionDenied", "The client has not shared this data category.", ErrorKind.Forbidden);
        AssertError(DietologistErrors.NoActiveRelationship, "Dietologist.NoActiveRelationship", "No active dietologist relationship found.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
