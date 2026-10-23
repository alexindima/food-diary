using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Queries.GetClientGoals;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationByToken;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Common;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistReadFailureTests {
    [Fact]
    public async Task GoalsAndInvitationPreserveUserLookupFailureAfterAccessSucceedsAsync() {
        var userId = UserId.New();
        var error = new Error("Users.EmailUnavailable", "Email lookup failed");
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        IDietologistUserContextService users = Substitute.For<IDietologistUserContextService>();
        users.GetAccessibleUserEmailAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Failure<string>(error));
        users.GetAccessibleProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Failure<UserDietologistProfileModel>(error));
        users.GetAccessibleProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Failure<UserDietologistProfileModel>(error));
        IDietologistInvitationReadModelRepository invitations = Substitute.For<IDietologistInvitationReadModelRepository>();

        Result<ClientGoalsModel> goals = await new GetClientGoalsQueryHandler(invitations, users, access)
            .Handle(new GetClientGoalsQuery(userId.Value, Guid.NewGuid()), CancellationToken.None);
        Result<DietologistInvitationForCurrentUserModel> invitation = await new GetInvitationForCurrentUserQueryHandler(invitations, users, TimeProvider.System, access)
            .Handle(new GetInvitationForCurrentUserQuery(userId.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(goals.IsFailure);
        Assert.Equal(error, goals.Error);
        Assert.True(invitation.IsFailure);
        Assert.Equal(error, invitation.Error);
        Assert.Empty(invitations.ReceivedCalls());
    }

    [Fact]
    public async Task TokenLookupRejectsEmptyInvitationBeforeReadingRepositoryAsync() {
        var userId = UserId.New();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        IDietologistInvitationReadModelRepository invitations = Substitute.For<IDietologistInvitationReadModelRepository>();
        var handler = new GetInvitationByTokenQueryHandler(invitations, Substitute.For<IDietologistUserContextService>(), TimeProvider.System, access);
        Result<InvitationModel> result = await handler.Handle(new GetInvitationByTokenQuery(userId.Value, Guid.Empty), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Empty(invitations.ReceivedCalls());
    }
}
