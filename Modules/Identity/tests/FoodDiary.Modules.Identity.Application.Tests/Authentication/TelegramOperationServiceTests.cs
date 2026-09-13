using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramOperationServiceTests {
    [Theory]
    [InlineData("missing")]
    [InlineData("conflict")]
    [InlineData("created")]
    public async Task Register_BindsOperationToAuthenticatedOwnerAndGeneration(string outcome) {
        var user = User.CreateTelegram(456, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        var operation = Guid.NewGuid();
        bool missing = string.Equals(outcome, "missing", StringComparison.Ordinal);
        bool created = string.Equals(outcome, "created", StringComparison.Ordinal);
        _identities.AuthenticateTelegramAsync(456, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(missing
            ? Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.NotFound()) : Result.Success(principal));
        _store.RegisterAsync(123, 1, user.Id.Value, principal.SecurityVersion, "payload", Arg.Any<CancellationToken>()).Returns(created ? operation : (Guid?)null);

        Result<Guid> result = await CreateService().RegisterAsync(1, 456, "payload", CancellationToken.None);

        Assert.Equal(created, result.IsSuccess);
        if (created) { Assert.Equal(operation, result.Value); }
        await _store.Received(missing ? 0 : 1).RegisterAsync(123, 1, user.Id.Value, principal.SecurityVersion, "payload", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Checkpoint_PropagatesAtomicStoreOutcome(bool stored) {
        var user = User.CreateTelegram(456, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        var lease = new TelegramOperationLease(Guid.NewGuid(), Guid.NewGuid(), user.Id.Value, principal.SecurityVersion, "private", Checkpoint: null, DateTime.UtcNow.AddMinutes(2));
        _store.GetLeaseAsync(123, lease.OperationId, lease.LeaseId, Arg.Any<CancellationToken>()).Returns(lease);
        _identities.GetAuthenticationPrincipalAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _store.CheckpointAsync(123, lease.OperationId, lease.LeaseId, "result", completed: true, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(stored);
        Result result = await CreateService().CheckpointAsync(lease.OperationId, lease.LeaseId, "result", completed: true, DateTime.UtcNow, CancellationToken.None);
        Assert.Equal(stored, result.IsSuccess);
        if (!stored) {
            Assert.Equal("Telegram.OperationConflict", result.Error.Code);
        }
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("empty")]
    [InlineData("oversized")]
    [InlineData("local-time")]
    [InlineData("future")]
    public async Task InvalidCheckpoint_DoesNotReadOrWriteTheLease(string scenario) {
        _policy.OperationsEnabled.Returns(!string.Equals(scenario, "disabled", StringComparison.Ordinal));
        string payload = scenario switch { "empty" => "", "oversized" => new string('я', 16385), _ => "checkpoint" };
        DateTime next = scenario switch { "local-time" => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified), "future" => DateTime.UtcNow.AddDays(2), _ => DateTime.UtcNow };
        Result result = await CreateService().CheckpointAsync(Guid.NewGuid(), Guid.NewGuid(), payload, completed: false, next, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Empty(_store.ReceivedCalls());
    }

    [Fact]
    public async Task Register_RejectsInvalidInputBeforeAuthentication() {
        Result<Guid> result = await CreateService().RegisterAsync(-1, 456, "payload", CancellationToken.None);
        Assert.Equal("Telegram.InvalidOperation", result.Error.Code);
        Assert.Empty(_identities.ReceivedCalls());
    }

    private readonly ITelegramOperationStore _store = Substitute.For<ITelegramOperationStore>();
    private readonly ITelegramOperationPolicy _policy = Substitute.For<ITelegramOperationPolicy>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();

    public TelegramOperationServiceTests() {
        _policy.BotId.Returns(123L);
        _policy.OperationsEnabled.Returns(returnThis: true);
    }

    [Fact]
    public async Task DisabledOperations_DoNotReadPayloadOrAuthenticate() {
        _policy.OperationsEnabled.Returns(returnThis: false);
        Assert.True((await CreateService().RegisterAsync(1, 456, "payload", CancellationToken.None)).IsFailure);
        Assert.True((await CreateService().AcquireAsync(Guid.NewGuid(), CancellationToken.None)).IsFailure);
        await _store.DidNotReceive().AcquireAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _identities.DidNotReceive().AuthenticateTelegramAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StaleGeneration_CancelsOnlyStaleOperationAndDoesNotExposePayload() {
        var user = User.CreateTelegram(456, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        var lease = new TelegramOperationLease(Guid.NewGuid(), Guid.NewGuid(), user.Id.Value,
            principal.SecurityVersion + 1, "private", Checkpoint: null, DateTime.UtcNow.AddMinutes(2));
        _store.AcquireAsync(123, lease.OperationId, Arg.Any<CancellationToken>()).Returns(lease);
        _identities.GetAuthenticationPrincipalAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));

        Result<TelegramOperationLease> result = await CreateService().AcquireAsync(lease.OperationId, CancellationToken.None);

        Assert.True(result.IsFailure);
        await _store.Received(1).CancelOperationAsync(123, lease.OperationId, Arg.Any<CancellationToken>());
        await _store.DidNotReceive().CancelUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidGeneration_ReturnsWorkWithoutChangingIdentity() {
        var user = User.CreateTelegram(456, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        var lease = new TelegramOperationLease(Guid.NewGuid(), Guid.NewGuid(), user.Id.Value,
            principal.SecurityVersion, "private", Checkpoint: null, DateTime.UtcNow.AddMinutes(2));
        _store.AcquireAsync(123, lease.OperationId, Arg.Any<CancellationToken>()).Returns(lease);
        _identities.GetAuthenticationPrincipalAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));

        Result<TelegramOperationLease> result = await CreateService().AcquireAsync(lease.OperationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(lease, result.Value);
    }

    [Fact]
    public async Task Checkpoint_AfterAccountBlocked_DoesNotPersistWork() {
        var lease = new TelegramOperationLease(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "private", Checkpoint: null, DateTime.UtcNow.AddMinutes(2));
        _store.GetLeaseAsync(123, lease.OperationId, lease.LeaseId, Arg.Any<CancellationToken>()).Returns(lease);
        _identities.GetAuthenticationPrincipalAsync(new UserId(lease.UserId), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.NotFound()));

        Result result = await CreateService().CheckpointAsync(lease.OperationId, lease.LeaseId, "result", completed: false, DateTime.UtcNow, CancellationToken.None);

        Assert.True(result.IsFailure);
        await _store.DidNotReceive().CheckpointAsync(Arg.Any<long>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private TelegramOperationService CreateService() => new(_store, _policy, _identities, TimeProvider.System);
}
