using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserTelegramAccountServiceTests {
    [Fact]
    public async Task BindOidcIdentityAsync_RejectsDifferentSubjectWithoutWriting() {
        var user = User.CreateTelegram(123, "hash");
        user.BindTelegramOidcIdentity("https://oauth.telegram.org", "original");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result result = await CreateService().BindOidcIdentityAsync(user.Id, 123,
            "https://oauth.telegram.org", "replacement", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("original", user.TelegramOidcSubject);
        await _writer.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private readonly IUserLookupRepository _lookup = Substitute.For<IUserLookupRepository>();
    private readonly IUserWriteRepository _writer = Substitute.For<IUserWriteRepository>();
    private readonly IUserSessionRevocationService _sessions = Substitute.For<IUserSessionRevocationService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task RegisterAsync_CreatesPasswordlessUserWithoutClaimingEmailOrAiConsent() {
        _hasher.Hash(Arg.Any<string>()).Returns("inaccessible-hash");
        User? created = null;
        _writer.AddAsync(Arg.Do<User>(value => created = value), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<User>()));

        Result<UserAuthenticationPrincipalModel> result = await CreateService().RegisterAsync(
            new UserTelegramRegistrationModel(123, "Alex", LastName: null, "ru", "Asia/Tbilisi", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(created);
        Assert.Null(created.Email);
        Assert.False(created.HasPassword);
        Assert.False(created.IsEmailConfirmed);
        Assert.Null(created.AiConsentAcceptedAt);
        Assert.Equal("Asia/Tbilisi", result.Value.User.TimeZoneId);
        Assert.True(result.Value.User.HasTelegramIdentity);
    }

    [Fact]
    public async Task RegisterAsync_ExistingDeletedIdentityCannotBeRecreated() {
        var user = User.CreateTelegram(123, "hash");
        user.MarkDeleted(DateTime.UtcNow);
        _lookup.GetByTelegramUserIdIncludingDeletedAsync(123, Arg.Any<CancellationToken>()).Returns(user);

        Result<UserAuthenticationPrincipalModel> result = await CreateService().RegisterAsync(
            new UserTelegramRegistrationModel(123, FirstName: null, LastName: null, "en", "UTC", DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _writer.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkAsync_LastLoginDoesNotMutateOrRevoke() {
        var user = User.CreateTelegram(123, "hash");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result result = await CreateService().UnlinkAsync(user.Id, 123, user.SecurityVersion, CancellationToken.None);

        Assert.Equal("User.LastSignInMethod", result.Error.Code);
        Assert.Equal(123, user.TelegramUserId);
        await _sessions.DidNotReceive().RevokeAllAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkAsync_StaleGenerationDoesNotRemoveCurrentBinding() {
        var user = User.CreateTelegram(123, "hash");
        user.LinkGoogleIdentity("https://accounts.google.com", "subject");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result result = await CreateService().UnlinkAsync(user.Id, 123, user.SecurityVersion + 1, CancellationToken.None);

        Assert.Equal("Authentication.TelegramProofStale", result.Error.Code);
        Assert.Equal(123, user.TelegramUserId);
        await _writer.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _sessions.DidNotReceive().RevokeAllAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkAsync_WithGoogleRevokesSessionsAndDoesNotRepeatMutation() {
        var user = User.CreateTelegram(123, "hash");
        user.LinkGoogleIdentity("https://accounts.google.com", "subject");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        UserTelegramAccountService service = CreateService();

        Assert.True((await service.UnlinkAsync(user.Id, 123, user.SecurityVersion, CancellationToken.None)).IsSuccess);
        Assert.True((await service.UnlinkAsync(user.Id, 123, user.SecurityVersion, CancellationToken.None)).IsSuccess);

        Assert.Null(user.TelegramUserId);
        await _sessions.Received(1).RevokeAllAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddVerifiedEmailAsync_ConflictDoesNotMergeAccounts() {
        var user = User.CreateTelegram(123, "hash");
        var owner = User.Create("used@example.com", "hash");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _lookup.GetByEmailIncludingDeletedAsync("used@example.com", Arg.Any<CancellationToken>()).Returns(owner);

        Result result = await CreateService().AddVerifiedEmailAsync(user.Id, "used@example.com", user.SecurityVersion, CancellationToken.None);

        Assert.Equal("User.EmailAlreadyExists", result.Error.Code);
        Assert.Null(user.Email);
        await _writer.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddVerifiedEmailAsync_StaleProofDoesNotAssignAddress() {
        var user = User.CreateTelegram(123, "hash");
        _lookup.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        Result result = await CreateService().AddVerifiedEmailAsync(user.Id, "backup@example.com", user.SecurityVersion + 1, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Null(user.Email);
        await _writer.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private UserTelegramAccountService CreateService() => new(_lookup, _writer, _sessions, _hasher, TimeProvider.System);
}
