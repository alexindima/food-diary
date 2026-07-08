using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.InviteDietologist;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public async Task InviteDietologist_WithNullUserId_ReturnsFailure() {
        InviteDietologistCommandHandler handler = CreateInviteHandler();

        Result result = await handler.Handle(
            new InviteDietologistCommand(UserId: null, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task InviteDietologist_WhenUserNotFound_ReturnsFailure() {
        InviteDietologistCommandHandler handler = CreateInviteHandler();

        Result result = await handler.Handle(
            new InviteDietologistCommand(Guid.NewGuid(), "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task InviteDietologist_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        InviteDietologistCommandHandler handler = CreateInviteHandler(
            userRepository: CreateAccessCheckedFailingDietologistUserContext(userId));

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task InviteDietologist_WhenInvitingSelf_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId, "user@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        InviteDietologistCommandHandler handler = CreateInviteHandler(userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "user@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task InviteDietologist_WhenAlreadyHasActiveDietologist_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        DietologistInvitation activeInvitation = CreateAcceptedInvitation(userId, UserId.New());
        invRepo.Seed(activeInvitation);

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task InviteDietologist_WhenPendingExists_ReturnsFailure() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        DietologistInvitation pending = CreatePendingInvitation(userId);
        invRepo.Seed(pending);

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task InviteDietologist_WithValidData_Succeeds() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);

        var invRepo = new InMemoryInvitationRepository();
        var emailSender = new FakeEmailSender();

        InviteDietologistCommandHandler handler = CreateInviteHandler(invitationRepository: invRepo, userRepository: userRepo, emailSender: emailSender);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(invRepo.Added);
        Assert.True(emailSender.SentCount > 0);
    }


    [Fact]
    public async Task InviteDietologist_WhenRegisteredDietologistExists_CreatesNotificationAndPushesUpdate() {
        var userId = UserId.New();
        User client = CreateUser(userId, "client@example.com");
        User dietologist = CreateUser(UserId.New(), "diet@example.com");

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        userRepo.Seed(dietologist);

        var invitationRepo = new InMemoryInvitationRepository();
        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            notificationRepository: notificationRepo,
            notificationPusher: notificationPusher);

        Result result = await handler.Handle(
            new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(notificationRepo.Added);
        Assert.True(notificationPusher.PushCalled);
        Assert.Equal(dietologist.Id, notificationRepo.Added[0].UserId);
    }


    [Fact]
    public async Task InviteDietologist_WhenEmailEnqueueFailsForUnregisteredDietologist_Throws() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(user);
        var invitationRepo = new InMemoryInvitationRepository();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            emailSender: new ThrowingEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
                CancellationToken.None));
    }


    [Fact]
    public async Task InviteDietologist_WhenEmailEnqueueFailsForRegisteredDietologist_ThrowsBeforeNotificationFallback() {
        var userId = UserId.New();
        User client = CreateUser(userId, "client@example.com");
        User dietologist = CreateUser(UserId.New(), "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(client);
        userRepo.Seed(dietologist);
        var invitationRepo = new InMemoryInvitationRepository();
        var notificationRepo = new InMemoryNotificationRepository();

        InviteDietologistCommandHandler handler = CreateInviteHandler(
            invitationRepository: invitationRepo,
            userRepository: userRepo,
            emailSender: new ThrowingEmailSender(),
            notificationRepository: notificationRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new InviteDietologistCommand(userId.Value, "diet@example.com", AllPermissions),
                CancellationToken.None));
        Assert.Empty(notificationRepo.Added);
    }

}
