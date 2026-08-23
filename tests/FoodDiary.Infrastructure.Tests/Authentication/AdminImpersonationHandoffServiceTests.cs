using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class AdminImpersonationHandoffServiceTests {
    [Fact]
    public async Task ConsumeCodeAsync_WithCreatedCode_ReturnsTokenOnlyOnce() {
        var timeProvider = new FixedTimeProvider();
        var store = new InMemoryAdminSsoCodeStore(timeProvider);
        var service = new AdminImpersonationHandoffService(store);
        string code = await service.CreateCodeAsync("impersonation-token");

        string? first = await service.ConsumeCodeAsync(code);
        string? second = await service.ConsumeCodeAsync(code);

        Assert.Equal("impersonation-token", first);
        Assert.Null(second);
    }

    [Fact]
    public async Task ProtocolSpecificCodes_CannotConsumeEachOther() {
        var timeProvider = new FixedTimeProvider();
        var store = new InMemoryAdminSsoCodeStore(timeProvider);
        var handoffService = new AdminImpersonationHandoffService(store);
        var ssoService = new AdminSsoService(store, timeProvider);
        var userId = UserId.New();
        string handoffCode = await handoffService.CreateCodeAsync("impersonation-token");
        AdminSsoCode ssoCode = await ssoService.CreateCodeAsync(userId);

        Assert.Null(await ssoService.ExchangeCodeAsync(handoffCode));
        Assert.Null(await handoffService.ConsumeCodeAsync(ssoCode.Code));
        Assert.Equal("impersonation-token", await handoffService.ConsumeCodeAsync(handoffCode));
        Assert.Equal(userId, await ssoService.ExchangeCodeAsync(ssoCode.Code));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
