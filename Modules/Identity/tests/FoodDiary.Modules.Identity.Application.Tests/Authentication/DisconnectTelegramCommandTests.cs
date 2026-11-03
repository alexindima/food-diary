using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class DisconnectTelegramCommandTests {
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();
    private readonly ITelegramOperationStore _operations = Substitute.For<ITelegramOperationStore>();
    private readonly IPostCommitActionQueue _postCommit = Substitute.For<IPostCommitActionQueue>();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_CancelsOnlyAfterSuccessfulAccountMutationAndCommit(bool success) {
        var userId = new UserId(Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        _accounts.DisconnectAsync(userId, 7, token).Returns(success ? Result.Success() : Result.Failure(new Error("Rejected", "Rejected")));
        Func<CancellationToken, Task>? afterCommit = null;
        _postCommit.When(queue => queue.Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => afterCommit = call.ArgAt<Func<CancellationToken, Task>>(1));

        Result result = await new DisconnectTelegramCommandHandler(_accounts, _operations, _postCommit)
            .Handle(new DisconnectTelegramCommand(userId.Value, 7), token);

        Assert.Equal(success, result.IsSuccess);
        await _accounts.Received(1).DisconnectAsync(userId, 7, token);
        await _operations.DidNotReceive().CancelUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        Assert.Equal(success, afterCommit is not null);
        if (afterCommit is not null) {
            await afterCommit(token);
            await _operations.Received(1).CancelUserAsync(userId, token);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1L)]
    public async Task Handle_RejectsMissingOrInvalidAuthenticatedGeneration(long? version) {
        Result result = await new DisconnectTelegramCommandHandler(_accounts, _operations, _postCommit)
            .Handle(new DisconnectTelegramCommand(Guid.NewGuid(), version), CancellationToken.None);
        Assert.True(result.IsFailure);
        await _accounts.DidNotReceive().DisconnectAsync(Arg.Any<UserId>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        _postCommit.DidNotReceive().Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>());
    }
}
