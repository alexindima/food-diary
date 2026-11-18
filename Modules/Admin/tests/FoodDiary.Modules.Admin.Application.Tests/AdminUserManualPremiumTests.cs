using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPremiumGrant;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserPremiumGrant;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminUserManualPremiumTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Set_RequiresBothActorAndTargetBeforeDispatchingOrSchedulingAudit(bool targetIsEmpty) {
        ISender users = Substitute.For<ISender>();
        IAuditLogger logger = Substitute.For<IAuditLogger>();
        IPostCommitActionQueue actions = Substitute.For<IPostCommitActionQueue>();
        var handler = new SetAdminUserPremiumGrantCommandHandler(users, logger, actions);
        var command = new SetAdminUserPremiumGrantCommand(
            targetIsEmpty ? Guid.Empty : Guid.NewGuid(),
            targetIsEmpty ? Guid.NewGuid() : Guid.Empty,
            DateTimeOffset.UtcNow.AddDays(1), "Test grant");

        Error error = ResultAssert.Failure(await handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Empty(users.ReceivedCalls());
        Assert.Empty(actions.ReceivedCalls());
        Assert.Empty(logger.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_RejectsEmptyIdsAndReturnsNotFoundForUnavailableUsers(bool emptyId) {
        ISender users = Substitute.For<ISender>();
        var handler = new GetAdminUserPremiumGrantQueryHandler(users);
        var userId = new UserId(emptyId ? Guid.Empty : Guid.NewGuid());

        Error error = ResultAssert.Failure(await handler.Handle(new GetAdminUserPremiumGrantQuery(userId.Value), CancellationToken.None));

        Assert.Equal(emptyId ? ErrorKind.Validation : ErrorKind.NotFound, error.Kind);
        await users.Received(emptyId ? 0 : 1).Send(new GetUserManualPremiumGrantQuery(userId), CancellationToken.None);
    }
}
