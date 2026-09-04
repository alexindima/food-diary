using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Compatibility;

[ExcludeFromCodeCoverage]
public sealed class ModuleErrorFacadeTests {
    [Theory]
    [InlineData(typeof(DailyAdviceErrors), "DailyAdvices")]
    [InlineData(typeof(DietologistErrors), "Dietologist")]
    [InlineData(typeof(FastingErrors), "Fasting")]
    [InlineData(typeof(HydrationEntryErrors), "Hydration")]
    [InlineData(typeof(MealErrors), "Meals")]
    public void ErrorFactory_HasModuleDeclaringAssembly(Type factory, string module) {
        Assert.Equal($"FoodDiary.Modules.{module}.Application.Abstractions", factory.Assembly.GetName().Name);
        Assert.Equal($"FoodDiary.Application.Abstractions.{module}.Common", factory.Namespace);
    }

    [Theory]
    [InlineData(null, "Daily advice items are not configured.")]
    [InlineData("", "Daily advice items are not configured for locale ''.")]
    [InlineData("ru", "Daily advice items are not configured for locale 'ru'.")]
    public void DailyAdvice_PreservesLocaleAndNotFound(string? locale, string message) {
        AssertError(DailyAdviceErrors.NotFound(locale), Errors.DailyAdvice.NotFound(locale),
            "DailyAdvice.NotFound", message, ErrorKind.NotFound);
        Assert.Equal(DailyAdviceErrors.NotFound(locale: null), Errors.DailyAdvice.NotFound());
    }

    [Fact]
    public void Dietologist_PreservesAllAccessAndInvitationErrors() {
        AssertError(DietologistErrors.InvitationNotFound, Errors.Dietologist.InvitationNotFound,
            "Dietologist.InvitationNotFound", "Dietologist invitation was not found.", ErrorKind.NotFound);
        AssertError(DietologistErrors.InvitationExpired, Errors.Dietologist.InvitationExpired,
            "Dietologist.InvitationExpired", "Dietologist invitation has expired.", ErrorKind.Validation);
        AssertError(DietologistErrors.InvitationInvalidToken, Errors.Dietologist.InvitationInvalidToken,
            "Dietologist.InvitationInvalidToken", "Invitation token is invalid.", ErrorKind.Unauthorized);
        AssertError(DietologistErrors.AlreadyHasDietologist, Errors.Dietologist.AlreadyHasDietologist,
            "Dietologist.AlreadyHasDietologist", "You already have an active dietologist.", ErrorKind.Conflict);
        AssertError(DietologistErrors.PendingInvitationExists, Errors.Dietologist.PendingInvitationExists,
            "Dietologist.PendingInvitationExists", "A pending invitation already exists.", ErrorKind.Conflict);
        AssertError(DietologistErrors.CannotInviteSelf, Errors.Dietologist.CannotInviteSelf,
            "Dietologist.CannotInviteSelf", "You cannot invite yourself as a dietologist.", ErrorKind.Validation);
        AssertError(DietologistErrors.AccessDenied, Errors.Dietologist.AccessDenied,
            "Dietologist.AccessDenied", "You do not have access to this client's data.", ErrorKind.Forbidden);
        AssertError(DietologistErrors.PermissionDenied, Errors.Dietologist.PermissionDenied,
            "Dietologist.PermissionDenied", "The client has not shared this data category.", ErrorKind.Forbidden);
        AssertError(DietologistErrors.NoActiveRelationship, Errors.Dietologist.NoActiveRelationship,
            "Dietologist.NoActiveRelationship", "No active dietologist relationship found.", ErrorKind.NotFound);
    }

    [Fact]
    public void Fasting_PreservesClassificationAndCustomMessage() {
        AssertError(FastingErrors.AlreadyActive, Errors.Fasting.AlreadyActive,
            "Fasting.AlreadyActive", "A fasting session is already active.", ErrorKind.Conflict);
        AssertError(FastingErrors.NoActiveSession, Errors.Fasting.NoActiveSession,
            "Fasting.NoActiveSession", "No active fasting session found.", ErrorKind.NotFound);
        AssertError(FastingErrors.InvalidProtocol, Errors.Fasting.InvalidProtocol,
            "Fasting.InvalidProtocol", "Invalid fasting protocol.", ErrorKind.Validation);
        AssertError(FastingErrors.InvalidCyclicAction("custom reason"), Errors.Fasting.InvalidCyclicAction("custom reason"),
            "Fasting.InvalidCyclicAction", "custom reason", ErrorKind.Validation);
    }

    [Fact]
    public void Hydration_PreservesNotFoundPrivacyAndRoundTripTimestamp() {
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var timestamp = new DateTime(2026, 9, 4, 1, 2, 3, DateTimeKind.Utc);
        AssertError(HydrationEntryErrors.NotFound(id), Errors.HydrationEntry.NotFound(id),
            "HydrationEntry.NotFound", "Hydration entry with id '12345678-1234-1234-1234-123456789abc' not found", ErrorKind.NotFound);
        AssertError(HydrationEntryErrors.NotAccessible(id), Errors.HydrationEntry.NotAccessible(id),
            "HydrationEntry.NotAccessible", "Hydration entry with id '12345678-1234-1234-1234-123456789abc' was not found or is not accessible.", ErrorKind.NotFound);
        AssertError(HydrationEntryErrors.AlreadyExists(timestamp), Errors.HydrationEntry.AlreadyExists(timestamp),
            "HydrationEntry.AlreadyExists", "A hydration entry already exists at '2026-09-04T01:02:03.0000000Z'.", ErrorKind.Conflict);
    }

    [Fact]
    public void Meals_PreservesNotFoundAndInternalClassification() {
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        AssertError(MealErrors.NotFound(id), Errors.Meal.NotFound(id),
            "Meal.NotFound", "Meal with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(MealErrors.InvalidData("invalid snapshot"), Errors.Meal.InvalidData("invalid snapshot"),
            "Meal.InvalidData", "invalid snapshot", ErrorKind.Internal);
    }

    private static void AssertError(Error owned, Error facade, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, owned.Code),
            () => Assert.Equal(message, owned.Message),
            () => Assert.Equal(kind, owned.Kind),
            () => Assert.Null(owned.Details),
            () => Assert.Equal(owned, facade));
    }
}
