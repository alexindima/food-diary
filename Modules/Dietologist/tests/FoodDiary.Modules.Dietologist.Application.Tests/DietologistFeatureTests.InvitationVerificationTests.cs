using FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitationForCurrentUser;
using FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitationForCurrentUser;
using FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

public partial class DietologistFeatureTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvitationWithoutToken_RequiresConfirmedMatchingMailbox(bool confirmed) {
        User user = CreateUser(email: "diet@example.com");
        user.SetEmailConfirmed(confirmed);
        var users = new InMemoryUserRepository();
        users.Seed(user);
        var invitations = new InMemoryInvitationRepository();
        DietologistInvitation invitation = CreatePendingInvitation(CreateUser().Id, "diet@example.com");
        invitations.Seed(invitation);
        IUserRoleMembershipService roles = Substitute.For<IUserRoleMembershipService>();

        Result<DietologistInvitationForCurrentUserModel> read = await CreateGetInvitationForCurrentUserHandler(invitations, users).Handle(
            new GetInvitationForCurrentUserQuery(user.Id.Value, invitation.Id.Value), CancellationToken.None);
        Result accept = await CreateAcceptCurrentUserHandler(invitations, users, roles).Handle(
            new AcceptInvitationForCurrentUserCommand(user.Id.Value, invitation.Id.Value), CancellationToken.None);

        Assert.Equal(confirmed, read.IsSuccess);
        Assert.Equal(confirmed, accept.IsSuccess);
        Assert.Equal(confirmed ? DietologistInvitationStatus.Accepted : DietologistInvitationStatus.Pending, invitation.Status);
        if (!confirmed) {
            Assert.Empty(roles.ReceivedCalls());
            Result decline = await CreateDeclineCurrentUserHandler(invitations, users).Handle(
                new DeclineInvitationForCurrentUserCommand(user.Id.Value, invitation.Id.Value), CancellationToken.None);
            Assert.False(decline.IsSuccess);
            Assert.Equal(DietologistInvitationStatus.Pending, invitation.Status);
        }
    }
}
