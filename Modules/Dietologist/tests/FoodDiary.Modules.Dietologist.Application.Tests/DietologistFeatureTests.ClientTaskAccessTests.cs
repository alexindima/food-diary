using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Application.Queries.GetClientTasksForDietologist;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

public partial class DietologistFeatureTests {
    [Theory]
    [InlineData(DietologistInvitationStatus.Pending)]
    [InlineData(DietologistInvitationStatus.Declined)]
    [InlineData(DietologistInvitationStatus.Revoked)]
    public async Task GetClientTasksForDietologist_WhenRelationshipIsInactive_DoesNotReadTasks(
        DietologistInvitationStatus status) {
        User dietologist = CreateUser(email: "diet@example.com");
        var clientId = UserId.New();
        DietologistInvitation invitation = status == DietologistInvitationStatus.Revoked
            ? CreateAcceptedInvitation(clientId, dietologist.Id)
            : CreatePendingInvitation(clientId);
        if (status == DietologistInvitationStatus.Revoked) {
            invitation.Revoke();
        } else if (status == DietologistInvitationStatus.Declined) {
            invitation.Decline();
        }

        var invitations = new InMemoryInvitationRepository();
        invitations.Seed(invitation);
        var users = new InMemoryUserRepository();
        users.Seed(dietologist);
        IClientTaskReadModelRepository tasks = Substitute.For<IClientTaskReadModelRepository>();
        var handler = new GetClientTasksForDietologistQueryHandler(tasks, invitations, users, TimeProvider.System);

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetClientTasksForDietologistQuery(dietologist.Id.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result, DietologistErrors.AccessDenied.Code);
        await tasks.DidNotReceive().GetByDietologistAndClientAsync(
            Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetClientTasksForDietologist_WhenRelationshipBelongsToAnotherPair_DoesNotReadTasks(bool foreignClient) {
        User dietologist = CreateUser(email: "diet@example.com");
        var clientId = UserId.New();
        var invitations = new InMemoryInvitationRepository();
        invitations.Seed(CreateAcceptedInvitation(
            foreignClient ? UserId.New() : clientId,
            foreignClient ? dietologist.Id : UserId.New()));
        var users = new InMemoryUserRepository();
        users.Seed(dietologist);
        IClientTaskReadModelRepository tasks = Substitute.For<IClientTaskReadModelRepository>();
        var handler = new GetClientTasksForDietologistQueryHandler(tasks, invitations, users, TimeProvider.System);

        Result<IReadOnlyList<ClientTaskModel>> result = await handler.Handle(
            new GetClientTasksForDietologistQuery(dietologist.Id.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result, DietologistErrors.AccessDenied.Code);
        await tasks.DidNotReceive().GetByDietologistAndClientAsync(
            Arg.Any<UserId>(), Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
