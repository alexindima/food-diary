using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.Logout;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeSession;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeOtherSessions;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class ActiveSessionManagementTests {
    private static readonly DateTime FixedNow = new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RevokeSession_ForwardsOwnerCurrentSessionClockAndCancellation() {
        var userId = new UserId(Guid.NewGuid());
        var currentSessionId = RefreshTokenSessionId.New();
        var targetSessionId = RefreshTokenSessionId.New();
        using var cancellation = new CancellationTokenSource();
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        var handler = new RevokeSessionCommandHandler(repository, new FixedTimeProvider());

        Result result = await handler.Handle(new RevokeSessionCommand(userId, currentSessionId, targetSessionId), cancellation.Token);

        ResultAssert.Success(result);
        await repository.Received(1).RevokeOtherByIdAsync(targetSessionId, userId, currentSessionId, FixedNow, cancellation.Token);
        await repository.DidNotReceiveWithAnyArgs().RevokeByIdAsync(default, default, default, default);
    }

    [Fact]
    public async Task RevokeOtherSessions_ForwardsOwnerAndPreservesCurrentSession() {
        var userId = new UserId(Guid.NewGuid());
        var currentSessionId = RefreshTokenSessionId.New();
        using var cancellation = new CancellationTokenSource();
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        var handler = new RevokeOtherSessionsCommandHandler(repository, new FixedTimeProvider());

        Result result = await handler.Handle(new RevokeOtherSessionsCommand(userId, currentSessionId), cancellation.Token);

        ResultAssert.Success(result);
        await repository.Received(1).RevokeAllOtherAsync(userId, currentSessionId, FixedNow, cancellation.Token);
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesSignedSession() {
        var userId = new UserId(Guid.NewGuid());
        var sessionId = RefreshTokenSessionId.New();
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
        IRefreshTokenSessionReadModelRepository repository = Substitute.For<IRefreshTokenSessionReadModelRepository>();
        repository.IsActiveAsync(userId: userId, sessionId: Arg.Any<RefreshTokenSessionId>(), cancellationToken: CancellationToken.None).Returns(returnThis: false);
        repository.GetActivePageReadModelsAsync(userId: userId, page: 1, limit: 50, cancellationToken: CancellationToken.None)
            .Returns(Task.FromResult<IReadOnlyList<RefreshTokenSessionReadModel>>([new(
                activeSession.Id, activeSession.AuthProvider, activeSession.UserAgent, activeSession.CreatedAtUtc, activeSession.LastRotatedAtUtc)]));
        var handler = new GetActiveSessionsQueryHandler(repository);

        Result<IReadOnlyList<ActiveSessionModel>> result = await handler.Handle(
            new GetActiveSessionsQuery(userId, RefreshTokenSessionId.New()),
            CancellationToken.None);

        ResultAssert.Failure(result, AuthenticationErrors.InvalidToken.Code);
    }

    [Fact]
    public async Task GetActiveSessions_WithActiveCurrentSession_MapsMinimizedDeviceMetadata() {
        var userId = new UserId(Guid.NewGuid());
        UserRefreshTokenSession activeSession = CreateSession(userId, Guid.NewGuid());
        IRefreshTokenSessionReadModelRepository repository = Substitute.For<IRefreshTokenSessionReadModelRepository>();
        repository.IsActiveAsync(userId: userId, sessionId: activeSession.Id, cancellationToken: CancellationToken.None).Returns(returnThis: true);
        repository.GetActivePageReadModelsAsync(userId: userId, page: 1, limit: 50, cancellationToken: CancellationToken.None)
            .Returns(Task.FromResult<IReadOnlyList<RefreshTokenSessionReadModel>>([new(
                activeSession.Id, activeSession.AuthProvider, activeSession.UserAgent, activeSession.CreatedAtUtc, activeSession.LastRotatedAtUtc)]));
        var handler = new GetActiveSessionsQueryHandler(repository);

        Result<IReadOnlyList<ActiveSessionModel>> result = await handler.Handle(
            new GetActiveSessionsQuery(userId, activeSession.Id),
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
            new RefreshTokenSessionId(sessionId),
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
