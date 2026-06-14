using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Options;
using System.Text;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class WearableOAuthStateServiceTests {
    [Fact]
    public void IsValidState_WithMatchingUserAndProvider_ReturnsTrue() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();

        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.True(service.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenStateIsTampered_ReturnsFalse() {
        var userId = UserId.New();
        WearableOAuthStateService service = CreateService();
        string state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState($"{state}x", userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenUserDiffers_ReturnsFalse() {
        WearableOAuthStateService service = CreateService();
        string state = service.CreateState(UserId.New(), WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState(state, UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void CreateState_WhenSecretKeyInvalid_Throws() {
        WearableOAuthStateService service = CreateService(secretKey: "");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            service.CreateState(UserId.New(), WearableProvider.Fitbit, "client-state"));

        Assert.Contains("SecretKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsValidState_WhenSecretKeyInvalid_ReturnsFalse() {
        WearableOAuthStateService service = CreateService(secretKey: "");

        Assert.False(service.IsValidState("state", UserId.New(), WearableProvider.Fitbit));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("payloadonly")]
    [InlineData(".signature")]
    [InlineData("payload.")]
    public void IsValidState_WhenStateShapeInvalid_ReturnsFalse(string state) {
        WearableOAuthStateService service = CreateService();

        Assert.False(service.IsValidState(state, UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenSignatureIsNotBase64Url_ReturnsFalse() {
        WearableOAuthStateService service = CreateService();

        Assert.False(service.IsValidState("payload.***", UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenPayloadIsNotBase64UrlAfterValidSignature_ReturnsFalse() {
        WearableOAuthStateService service = CreateService();
        const string payloadSegment = "***";
        string signatureSegment = SignStatePayload(service, payloadSegment);

        Assert.False(service.IsValidState($"{payloadSegment}.{signatureSegment}", UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenPayloadJsonInvalidAfterValidSignature_ReturnsFalse() {
        WearableOAuthStateService service = CreateService();
        string payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes("not-json"));
        string signatureSegment = SignStatePayload(service, payloadSegment);

        Assert.False(service.IsValidState($"{payloadSegment}.{signatureSegment}", UserId.New(), WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenStateExpired_ReturnsFalse() {
        var userId = UserId.New();
        WearableOAuthStateService issuer = CreateService(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc));
        string state = issuer.CreateState(userId, WearableProvider.Fitbit, "client-state");
        WearableOAuthStateService validator = CreateService(new DateTime(2026, 5, 31, 0, 11, 0, DateTimeKind.Utc));

        Assert.False(validator.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    private static WearableOAuthStateService CreateService(DateTime? utcNow = null, string? secretKey = null) =>
        new(
            MsOptions.Create(new JwtOptions {
                SecretKey = secretKey ?? "super-secret-key-for-tests-only-123456789",
                Issuer = "FoodDiary",
                Audience = "FoodDiaryClients",
                ExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7,
                RememberMeRefreshTokenExpirationDays = 90,
            }),
            new StubDateTimeProvider(utcNow ?? new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)));

    private static string SignStatePayload(WearableOAuthStateService service, string payloadSegment) {
        System.Reflection.MethodInfo signMethod = typeof(WearableOAuthStateService).GetMethod(
            "Sign",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        byte[] signature = (byte[])signMethod.Invoke(service, [payloadSegment])!;
        return Base64UrlEncode(signature);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
