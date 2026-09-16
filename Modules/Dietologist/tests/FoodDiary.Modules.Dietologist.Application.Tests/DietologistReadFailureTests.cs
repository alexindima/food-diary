using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Queries.GetClientGoals;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationByToken;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Common;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistReadFailureTests {
    [Fact]
    public async Task GoalsAndInvitationPreserveEmailFailureAfterAccessSucceedsAsync() {
        var userId = UserId.New();
        var error = new Error("Users.EmailUnavailable", "Email lookup failed");
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        IDietologistUserContextService users = Substitute.For<IDietologistUserContextService>();
        users.GetAccessibleUserEmailAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Failure<string>(error));
        IDietologistInvitationReadModelRepository invitations = Substitute.For<IDietologistInvitationReadModelRepository>();

        FoodDiary.Results.Result<FoodDiary.Modules.Users.Contracts.Models.UserModel> goals = await new GetClientGoalsQueryHandler(invitations, users, access)
            .Handle(new GetClientGoalsQuery(userId.Value, Guid.NewGuid()), CancellationToken.None);
        FoodDiary.Results.Result<FoodDiary.Modules.Dietologist.Application.Models.DietologistInvitationForCurrentUserModel> invitation = await new GetInvitationForCurrentUserQueryHandler(invitations, users, TimeProvider.System, access)
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
        FoodDiary.Results.Result<FoodDiary.Modules.Dietologist.Application.Models.InvitationModel> result = await handler.Handle(new GetInvitationByTokenQuery(userId.Value, Guid.Empty), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Empty(invitations.ReceivedCalls());
    }
}
