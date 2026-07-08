using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;
using FoodDiary.Application.Dietologist.Commands.RevokeInvitation;
using FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public async Task RevokeInvitation_WithNullUserId_ReturnsFailure() {
        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new RevokeInvitationCommand(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task RevokeInvitation_WhenNothingToRevoke_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task RevokeInvitation_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new RevokeInvitationCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task RevokeInvitation_WithPendingInvitation_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreatePendingInvitation(userId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DietologistInvitationStatus.Revoked, invitation.Status);
    }


    [Fact]
    public async Task RevokeInvitation_WithActiveInvitation_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new RevokeInvitationCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new RevokeInvitationCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
    }


    [Fact]
    public async Task DisconnectDietologist_WithNullUserId_ReturnsFailure() {
        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task DisconnectDietologist_WhenNoRelationship_ReturnsFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task DisconnectDietologist_WithEmptyClientUserId_ReturnsValidationFailure() {
        var dietologistId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        var handler = new DisconnectDietologistCommandHandler(new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("ClientUserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DisconnectDietologist_WhenUserDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId));

        var handler = new DisconnectDietologistCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task DisconnectDietologist_WithActiveRelationship_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        User user = CreateUser(dietologistId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new DisconnectDietologistCommandHandler(invRepo, userRepo);

        Result result = await handler.Handle(
            new DisconnectDietologistCommand(dietologistId.Value, clientId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
    }


    [Fact]
    public async Task UpdatePermissions_WithNullUserId_ReturnsFailure() {
        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(UserId: null, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task UpdatePermissions_WhenNoActiveRelationship_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task UpdatePermissions_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(userId));

        var handler = new UpdateDietologistPermissionsCommandHandler(
            new InMemoryInvitationRepository(), userRepo);

        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task UpdatePermissions_WithActiveRelationship_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        DietologistInvitation invitation = CreateAcceptedInvitation(userId, UserId.New());
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var handler = new UpdateDietologistPermissionsCommandHandler(invRepo, userRepo);

        var newPermissions = new DietologistPermissionsInput(ShareMeals: false, ShareStatistics: false, ShareWeight: true, ShareWaist: true, ShareGoals: true, ShareHydration: true);
        Result result = await handler.Handle(
            new UpdateDietologistPermissionsCommand(userId.Value, newPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

}
