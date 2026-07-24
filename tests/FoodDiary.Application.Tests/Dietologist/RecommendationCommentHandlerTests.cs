using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class RecommendationCommentHandlerTests {
    [Fact]
    public async Task CreateRecommendationComment_WhenClientOwnsRecommendation_AddsCommentAndNotification() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        var recommendation = Recommendation.Create(dietologistId, clientId, "Recommendation");
        var client = User.Create("client@example.com", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(client, clientId);
        IRecommendationCommentRepository comments = Substitute.For<IRecommendationCommentRepository>();
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        IUserContextService users = CreateAccessibleUserContext(client);
        var handler = new CreateRecommendationCommentCommandHandler(
            CreateRecommendationRepository(recommendation),
            comments,
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            notifications,
            Substitute.For<IAuditEntryWriter>(),
            users);

        Result<FoodDiary.Application.Dietologist.Models.RecommendationCommentModel> result = await handler.Handle(
            new CreateRecommendationCommentCommand(clientId.Value, recommendation.Id.Value, "  My question  "),
            CancellationToken.None);

        ResultAssert.Success(result);
        FoodDiary.Application.Dietologist.Models.RecommendationCommentModel model = Assert.IsType<
            FoodDiary.Application.Dietologist.Models.RecommendationCommentModel>(result.Value);
        Assert.Multiple(
            () => Assert.Equal("My question", model.Text),
            () => Assert.Equal(clientId.Value, model.AuthorUserId),
            () => Assert.Equal("client@example.com", model.AuthorEmail));
        await comments.Received(1).AddAsync(
            Arg.Is<RecommendationComment>(comment =>
                comment != null &&
                comment.RecommendationId == recommendation.Id &&
                comment.AuthorUserId == clientId &&
                comment.Text == "My question"),
            Arg.Any<CancellationToken>());
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification != null && notification.UserId == dietologistId),
            sendWebPush: false,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRecommendationComment_WhenDietologistRelationshipIsInactive_ReturnsAccessDenied() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        var recommendation = Recommendation.Create(dietologistId, clientId, "Recommendation");
        var dietologist = User.Create("dietologist@example.com", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(dietologist, dietologistId);
        IRecommendationCommentRepository comments = Substitute.For<IRecommendationCommentRepository>();
        var handler = new CreateRecommendationCommentCommandHandler(
            CreateRecommendationRepository(recommendation),
            comments,
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            Substitute.For<INotificationWriter>(),
            Substitute.For<IAuditEntryWriter>(),
            CreateAccessibleUserContext(dietologist));

        Result<FoodDiary.Application.Dietologist.Models.RecommendationCommentModel> result = await handler.Handle(
            new CreateRecommendationCommentCommand(dietologistId.Value, recommendation.Id.Value, "Reply"),
            CancellationToken.None);

        ResultAssert.Failure(result, "Dietologist.AccessDenied");
        await comments.DidNotReceive().AddAsync(
            Arg.Any<RecommendationComment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRecommendationComment_WhenUserIsNotParticipant_DoesNotRevealRecommendation() {
        var clientId = UserId.New();
        var dietologistId = UserId.New();
        var outsider = User.Create("outsider@example.com", "hash");
        var recommendation = Recommendation.Create(dietologistId, clientId, "Recommendation");
        var handler = new CreateRecommendationCommentCommandHandler(
            CreateRecommendationRepository(recommendation),
            Substitute.For<IRecommendationCommentRepository>(),
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            Substitute.For<INotificationWriter>(),
            Substitute.For<IAuditEntryWriter>(),
            CreateAccessibleUserContext(outsider));

        Result<FoodDiary.Application.Dietologist.Models.RecommendationCommentModel> result = await handler.Handle(
            new CreateRecommendationCommentCommand(outsider.Id.Value, recommendation.Id.Value, "Reply"),
            CancellationToken.None);

        ResultAssert.Failure(result, "Dietologist.InvitationNotFound");
    }

    [Fact]
    public async Task GetRecommendationComments_WhenParticipant_ReturnsRepositoryOrder() {
        var clientId = UserId.New();
        var recommendation = Recommendation.Create(UserId.New(), clientId, "Recommendation");
        var expected = new List<RecommendationCommentReadModel> {
            new(Guid.NewGuid(), recommendation.Id.Value, clientId.Value, "Client", null, "client@example.com", "First", DateTime.UtcNow.AddMinutes(-1)),
            new(Guid.NewGuid(), recommendation.Id.Value, recommendation.DietologistUserId.Value, "Dietologist", null, "dietologist@example.com", "Second", DateTime.UtcNow),
        };
        IRecommendationCommentRepository comments = Substitute.For<IRecommendationCommentRepository>();
        comments.GetByRecommendationAsync(recommendation.Id, Arg.Any<CancellationToken>())
            .Returns(expected);
        var handler = new GetRecommendationCommentsQueryHandler(
            new RecommendationDiscussionReadService(CreateRecommendationRepository(recommendation), comments),
            CreateCurrentUserAccess());

        Result<IReadOnlyList<FoodDiary.Application.Dietologist.Models.RecommendationCommentModel>> result =
            await handler.Handle(
                new GetRecommendationCommentsQuery(clientId.Value, recommendation.Id.Value),
                CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value,
            first => Assert.Equal("First", first.Text),
            second => Assert.Equal("Second", second.Text));
    }

    [Fact]
    public async Task GetRecommendationComments_WhenUserIsNotParticipant_ReturnsFailure() {
        var recommendation = Recommendation.Create(UserId.New(), UserId.New(), "Recommendation");
        var handler = new GetRecommendationCommentsQueryHandler(
            new RecommendationDiscussionReadService(
                CreateRecommendationRepository(recommendation),
                Substitute.For<IRecommendationCommentRepository>()),
            CreateCurrentUserAccess());

        Result<IReadOnlyList<FoodDiary.Application.Dietologist.Models.RecommendationCommentModel>> result =
            await handler.Handle(
                new GetRecommendationCommentsQuery(Guid.NewGuid(), recommendation.Id.Value),
                CancellationToken.None);

        ResultAssert.Failure(result, "Dietologist.InvitationNotFound");
    }

    private static IRecommendationReadRepository CreateRecommendationRepository(Recommendation recommendation) {
        IRecommendationReadRepository repository = Substitute.For<IRecommendationReadRepository>();
        repository.GetByIdAsync(recommendation.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(recommendation);
        return repository;
    }

    private static IUserContextService CreateAccessibleUserContext(User user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        service.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        return service;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccess() {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        return service;
    }
}
