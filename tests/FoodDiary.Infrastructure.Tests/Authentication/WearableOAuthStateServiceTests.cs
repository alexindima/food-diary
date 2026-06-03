using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class WearableOAuthStateServiceTests {
    [Fact]
    public void IsValidState_WithMatchingUserAndProvider_ReturnsTrue() {
        var userId = UserId.New();
        var service = CreateService();

        var state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.True(service.IsValidState(state, userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenStateIsTampered_ReturnsFalse() {
        var userId = UserId.New();
        var service = CreateService();
        var state = service.CreateState(userId, WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState($"{state}x", userId, WearableProvider.Fitbit));
    }

    [Fact]
    public void IsValidState_WhenUserDiffers_ReturnsFalse() {
        var service = CreateService();
        var state = service.CreateState(UserId.New(), WearableProvider.Fitbit, "client-state");

        Assert.False(service.IsValidState(state, UserId.New(), WearableProvider.Fitbit));
    }

    private static WearableOAuthStateService CreateService(DateTime? utcNow = null) =>
        new(
            MsOptions.Create(new JwtOptions {
                SecretKey = "super-secret-key-for-tests-only-123456789",
                Issuer = "FoodDiary",
                Audience = "FoodDiaryClients",
                ExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7,
            }),
            new StubDateTimeProvider(utcNow ?? new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc)));

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow => utcNow;
    }
}
