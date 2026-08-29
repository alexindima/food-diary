using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Identity.Authentication.Commands.Logout;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Queries.GetActiveSessions;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class ActiveSessionManagementTests {
    private static readonly DateTime FixedNow = new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesSignedSession() {
        var userId = new UserId(Guid.NewGuid());
        var sessionId = Guid.NewGuid();
        IJwtTokenGenerator tokens = Substitute.For<IJwtTokenGenerator>();
        tokens.ValidateToken("refresh-token").Returns((userId, "user@example.com", false, sessionId));
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        var handler = new LogoutCommandHandler(tokens, repository, new FixedTimeProvider());

        Result result = await handler.Handle(new LogoutCommand("refresh-token"), CancellationToken.None);

        ResultAssert.Success(result);
        await repository.Received(1).RevokeByIdAsync(sessionId, userId, FixedNow, CancellationToken.None);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-token")]
    public async Task Logout_WithoutResolvableSession_RemainsIdempotent(string? refreshToken) {
        IJwtTokenGenerator tokens = Substitute.For<IJwtTokenGenerator>();
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        var handler = new LogoutCommandHandler(tokens, repository, new FixedTimeProvider());

        Result result = await handler.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        ResultAssert.Success(result);
        await repository.DidNotReceiveWithAnyArgs().RevokeByIdAsync(
            default,
            default,
            default,
            default);
    }

    [Fact]
    public async Task GetActiveSessions_WhenClaimedCurrentSessionIsNotActive_RejectsStaleAccessToken() {
        var userId = new UserId(Guid.NewGuid());
        UserRefreshTokenSession activeSession = CreateSession(userId, Guid.NewGuid());
        IRefreshTokenSessionReadRepository repository = Substitute.For<IRefreshTokenSessionReadRepository>();
        repository.GetActiveByUserIdAsync(userId, CancellationToken.None)
            .Returns(Task.FromResult<IReadOnlyList<UserRefreshTokenSession>>([activeSession]));
        var handler = new GetActiveSessionsQueryHandler(repository);

        Result<IReadOnlyList<ActiveSessionModel>> result = await handler.Handle(
            new GetActiveSessionsQuery(userId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
    }

    [Fact]
    public async Task GetActiveSessions_WithActiveCurrentSession_MapsMinimizedDeviceMetadata() {
        var userId = new UserId(Guid.NewGuid());
        UserRefreshTokenSession activeSession = CreateSession(userId, Guid.NewGuid());
        IRefreshTokenSessionReadRepository repository = Substitute.For<IRefreshTokenSessionReadRepository>();
        repository.GetActiveByUserIdAsync(userId, CancellationToken.None)
            .Returns(Task.FromResult<IReadOnlyList<UserRefreshTokenSession>>([activeSession]));
        var handler = new GetActiveSessionsQueryHandler(repository);

        Result<IReadOnlyList<ActiveSessionModel>> result = await handler.Handle(
            new GetActiveSessionsQuery(userId.Value, activeSession.Id),
            CancellationToken.None);

        ActiveSessionModel model = Assert.Single(ResultAssert.Success(result));
        Assert.Multiple(
            () => Assert.True(model.IsCurrent),
            () => Assert.Equal("Chrome", model.Browser),
            () => Assert.Equal("Windows", model.OperatingSystem),
            () => Assert.Equal("Desktop", model.DeviceType));
    }

    private static UserRefreshTokenSession CreateSession(UserId userId, Guid sessionId) =>
        UserRefreshTokenSession.Create(
            sessionId,
            userId,
            "refresh-hash",
            rememberMe: false,
            authProvider: "password",
            ipAddress: "203.0.113.10",
            userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/125.0.0.0 Safari/537.36",
            FixedNow);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(FixedNow);
    }
}
