using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class BulkCreateRecommendationsHandlerTests {
    [Fact]
    public async Task Handle_WhenSomeRelationshipsAreInactive_ReturnsPartialSuccess() {
        var dietologist = User.Create("dietologist@example.com", "hash");
        var activeClientId = UserId.New();
        var inactiveClientId = UserId.New();
        IRecommendationRepository recommendations = Substitute.For<IRecommendationRepository>();
        IRecommendationBulkDispatchRepository dispatches = Substitute.For<IRecommendationBulkDispatchRepository>();
        dispatches.GetExistingAsync(
                dietologist.Id,
                "request-1",
                Arg.Any<IReadOnlyCollection<UserId>>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        IDietologistInvitationReadModelRepository invitations = CreateInvitationRepository(
            dietologist.Id,
            activeClientId);
        var handler = new BulkCreateRecommendationsCommandHandler(
            recommendations,
            dispatches,
            invitations,
            CreateUserContext(dietologist));

        Result<FoodDiary.Application.Dietologist.Models.BulkRecommendationResultModel> result = await handler.Handle(
            new BulkCreateRecommendationsCommand(
                dietologist.Id.Value,
                [activeClientId.Value, inactiveClientId.Value],
                "Recommendation",
                "request-1"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value.Recipients,
            recipient => Assert.Multiple(
                () => Assert.Equal(activeClientId.Value, recipient.ClientUserId),
                () => Assert.True(recipient.Succeeded),
                () => Assert.False(recipient.WasAlreadyProcessed)),
            recipient => Assert.Multiple(
                () => Assert.Equal(inactiveClientId.Value, recipient.ClientUserId),
                () => Assert.False(recipient.Succeeded),
                () => Assert.Equal("Dietologist.AccessDenied", recipient.ErrorCode)));
        await recommendations.Received(1).AddAsync(
            Arg.Is<Recommendation>(recommendation =>
                recommendation != null && recommendation.ClientUserId == activeClientId),
            Arg.Any<CancellationToken>());
        await dispatches.Received(1).AddAsync(
            Arg.Is<RecommendationBulkDispatch>(dispatch =>
                dispatch != null && dispatch.ClientUserId == activeClientId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRecipientWasAlreadyProcessed_DoesNotCreateDuplicate() {
        var dietologist = User.Create("dietologist@example.com", "hash");
        var clientId = UserId.New();
        var existingRecommendationId = RecommendationId.New();
        IRecommendationRepository recommendations = Substitute.For<IRecommendationRepository>();
        IRecommendationBulkDispatchRepository dispatches = Substitute.For<IRecommendationBulkDispatchRepository>();
        dispatches.GetExistingAsync(
                dietologist.Id,
                "request-2",
                Arg.Any<IReadOnlyCollection<UserId>>(),
                Arg.Any<CancellationToken>())
            .Returns([
                new RecommendationBulkDispatchReadModel(
                    clientId.Value,
                    existingRecommendationId.Value),
            ]);
        var handler = new BulkCreateRecommendationsCommandHandler(
            recommendations,
            dispatches,
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            CreateUserContext(dietologist));

        Result<FoodDiary.Application.Dietologist.Models.BulkRecommendationResultModel> result = await handler.Handle(
            new BulkCreateRecommendationsCommand(
                dietologist.Id.Value,
                [clientId.Value],
                "Recommendation",
                "request-2"),
            CancellationToken.None);

        ResultAssert.Success(result);
        BulkRecommendationRecipientResultModel recipient = Assert.Single(result.Value.Recipients);
        Assert.Multiple(
            () => Assert.True(recipient.Succeeded),
            () => Assert.True(recipient.WasAlreadyProcessed),
            () => Assert.Equal(existingRecommendationId.Value, recipient.RecommendationId));
        await recommendations.DidNotReceive().AddAsync(
            Arg.Any<Recommendation>(),
            Arg.Any<CancellationToken>());
        await dispatches.DidNotReceive().AddAsync(
            Arg.Any<RecommendationBulkDispatch>(),
            Arg.Any<CancellationToken>());
    }

    private static IDietologistInvitationReadModelRepository CreateInvitationRepository(
        UserId dietologistId,
        UserId activeClientId) {
        IDietologistInvitationReadModelRepository repository = Substitute.For<IDietologistInvitationReadModelRepository>();
        repository.GetActiveByClientAndDietologistReadModelAsync(
                activeClientId,
                dietologistId,
                Arg.Any<CancellationToken>())
            .Returns(CreateInvitation(dietologistId, activeClientId));
        return repository;
    }

    private static DietologistInvitationReadModel CreateInvitation(UserId dietologistId, UserId clientId) =>
        new(
            InvitationId: Guid.NewGuid(),
            ClientUserId: clientId.Value,
            DietologistUserId: dietologistId.Value,
            DietologistEmail: "dietologist@example.com",
            ClientEmail: "client@example.com",
            ClientFirstName: "Client",
            ClientLastName: null,
            ClientProfileImage: null,
            ClientBirthDate: null,
            ClientGender: null,
            ClientHeight: null,
            ClientActivityLevel: ActivityLevel.Moderate,
            DietologistUserEmail: "dietologist@example.com",
            DietologistFirstName: "Dietologist",
            DietologistLastName: null,
            Status: DietologistInvitationStatus.Accepted,
            Permissions: new DietologistPermissionsReadModel(
                ShareMeals: true,
                ShareStatistics: true,
                ShareWeight: true,
                ShareWaist: true,
                ShareGoals: true,
                ShareHydration: true,
                ShareProfile: true,
                ShareFasting: true),
            CreatedAtUtc: DateTime.UtcNow,
            ExpiresAtUtc: DateTime.UtcNow.AddDays(1),
            AcceptedAtUtc: DateTime.UtcNow);

    private static IUserContextService CreateUserContext(User user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        service.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        return service;
    }
}
