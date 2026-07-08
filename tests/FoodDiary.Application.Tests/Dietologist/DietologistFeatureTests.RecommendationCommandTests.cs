using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendation;
using FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;
using FoodDiary.Application.Dietologist.EventHandlers;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Tests.Dietologist;

public partial class DietologistFeatureTests {

    [Fact]
    public async Task CreateRecommendation_WithNullUserId_ReturnsFailure() {
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler();

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(UserId: null, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task CreateRecommendation_WhenNoAccess_ReturnsFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task CreateRecommendation_WhenDietologistLoadFailsAfterAccessCheck_ReturnsFailure() {
        var dietologistId = UserId.New();
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(
            userRepository: CreateAccessCheckedFailingDietologistUserContext(dietologistId));

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, Guid.NewGuid(), "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task CreateRecommendation_WithEmptyClientUserId_ReturnsValidationFailure() {
        var dietologistId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, Guid.Empty, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("ClientUserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task CreateRecommendation_WhenDietologistDeleted_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(CreateAcceptedInvitation(clientId, dietologistId));

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(dietologistId, "diet@example.com"));

        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(invitationRepository: invRepo, userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
    }


    [Fact]
    public async Task CreateRecommendation_WithAccess_Succeeds() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var recRepo = new InMemoryRecommendationRepository();
        var notificationRepo = new InMemoryNotificationRepository();
        var notificationPusher = new FakeNotificationPusher();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(invRepo, recRepo, notificationRepo, notificationPusher, userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Eat more veggies", result.Value.Text);
        Assert.Single(recRepo.Added);
        Notification notification = Assert.Single(notificationRepo.Added);
        Assert.Equal(NotificationTypes.NewRecommendation, notification.Type);
        Assert.True(notificationPusher.PushCalled);
    }


    [Fact]
    public async Task CreateRecommendation_WhenDietologistMissingDuringNotification_UsesEmptyLabel() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);

        var notificationRepo = new InMemoryNotificationRepository();
        var userRepo = new SequenceUserRepository(CreateUser(dietologistId, "diet@example.com"), null);
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(
            invitationRepository: invRepo,
            notificationRepository: notificationRepo,
            userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Success(result);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(
            Assert.Single(notificationRepo.Added).PayloadJson);
        Assert.Equal("diet@example.com", payload?.DietologistName);
    }


    [Fact]
    public async Task CreateRecommendation_WhenAnyPermissionIsMissing_ReturnsFailure() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var limitedPermissions = new DietologistPermissions(
            ShareMeals: true,
            ShareStatistics: true,
            ShareWeight: true,
            ShareWaist: true,
            ShareGoals: true,
            ShareHydration: true,
            ShareProfile: false,
            ShareFasting: true);
        DietologistInvitation invitation = CreateAcceptedInvitation(clientId, dietologistId, limitedPermissions);
        var invRepo = new InMemoryInvitationRepository();
        invRepo.Seed(invitation);
        var recRepo = new InMemoryRecommendationRepository();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(dietologistId, "diet@example.com"));
        CreateRecommendationCommandHandler handler = CreateRecommendationHandler(
            invitationRepository: invRepo,
            recommendationRepository: recRepo,
            userRepository: userRepo);

        Result<RecommendationModel> result = await handler.Handle(
            new CreateRecommendationCommand(dietologistId.Value, clientId.Value, "Eat more veggies"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("PermissionDenied", result.Error.Code, StringComparison.Ordinal);
        Assert.Empty(recRepo.Added);
    }


    [Fact]
    public async Task MarkRecommendationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkRecommendationReadCommandHandler(
            new InMemoryRecommendationRepository(), new InMemoryUserRepository());

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task MarkRecommendationRead_WhenNotFound_ReturnsFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(userId));
        var handler = new MarkRecommendationReadCommandHandler(new InMemoryRecommendationRepository(), userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(userId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task MarkRecommendationRead_WithEmptyRecommendationId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(userId));
        var handler = new MarkRecommendationReadCommandHandler(new InMemoryRecommendationRepository(), userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(userId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("RecommendationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task MarkRecommendationRead_WhenNotOwned_ReturnsFailure() {
        var clientId = UserId.New();
        var otherId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(otherId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(otherId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }


    [Fact]
    public async Task MarkRecommendationRead_WithValidOwner_Succeeds() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateUser(clientId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(clientId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(recommendation.IsRead);
    }


    [Fact]
    public async Task MarkRecommendationRead_WhenUserDeleted_ReturnsFailure() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "text");
        var recRepo = new InMemoryRecommendationRepository();
        recRepo.Seed(recommendation);

        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(CreateDeletedUser(clientId));
        var handler = new MarkRecommendationReadCommandHandler(recRepo, userRepo);

        Result result = await handler.Handle(
            new MarkRecommendationReadCommand(clientId.Value, recommendation.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AccountDeleted", result.Error.Code, StringComparison.Ordinal);
        Assert.False(recommendation.IsRead);
    }


    [Fact]
    public async Task RecommendationCreatedEventHandler_CreatesNotificationAndPushes() {
        var dietologistId = UserId.New();
        var clientId = UserId.New();
        var recId = RecommendationId.New();

        User dietologist = CreateUser(dietologistId, "diet@example.com");
        var userRepo = new InMemoryUserRepository();
        userRepo.Seed(dietologist);

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo,
            new InMemoryNotificationWriter(notifRepo, new FakeWebPushNotificationSender()),
            pusher,
            userRepo,
            new ImmediatePostCommitActionQueue());
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(new NotificationEnvelope<RecommendationCreatedDomainEvent>(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(notifRepo.Added[0].PayloadJson);
        Assert.Equal("diet@example.com", payload?.DietologistName);
        Assert.True(pusher.PushCalled);
    }


    [Fact]
    public async Task RecommendationCreatedEventHandler_WhenDietologistNotFound_UsesEmptyName() {
        var recId = RecommendationId.New();
        var clientId = UserId.New();
        var dietologistId = UserId.New();

        var notifRepo = new InMemoryNotificationRepository();
        var pusher = new FakeNotificationPusher();

        var handler = new RecommendationCreatedEventHandler(
            notifRepo,
            new InMemoryNotificationWriter(notifRepo, new FakeWebPushNotificationSender()),
            pusher,
            new InMemoryUserRepository(),
            new ImmediatePostCommitActionQueue());
        var domainEvent = new RecommendationCreatedDomainEvent(recId, dietologistId, clientId);

        await handler.Handle(new NotificationEnvelope<RecommendationCreatedDomainEvent>(domainEvent), CancellationToken.None);

        Assert.Single(notifRepo.Added);
        NewRecommendationNotificationPayload? payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(notifRepo.Added[0].PayloadJson);
        Assert.Equal(string.Empty, payload?.DietologistName);
    }


    [Fact]
    public void NotificationTargetUrlResolver_ForRecommendation_ReturnsRecommendationsPage() {
        string recommendationId = Guid.NewGuid().ToString();

        string? targetUrl = NotificationTargetUrlResolver.Resolve(NotificationTypes.NewRecommendation, recommendationId);

        Assert.Equal($"/recommendations?recommendationId={recommendationId}", targetUrl);
    }

}
