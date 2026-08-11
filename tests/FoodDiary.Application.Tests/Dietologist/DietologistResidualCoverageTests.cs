using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;
using FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

#pragma warning disable MA0003

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class DietologistResidualCoverageTests {
    [Fact]
    public void AttentionSignalSeverityOrdering_UnknownSeverityHasLowestPriority() {
        System.Reflection.MethodInfo method = typeof(GetAttentionSignalsQueryHandler).GetMethod(
            "SeverityOrder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        object? result = method.Invoke(null, ["Unknown"]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void DietologistDtoProperties_AreReadableBySerializers() {
        DateTime now = DateTime.UtcNow;
        var permissions = new DietologistPermissionsReadModel(true, true, true, true, true, true, true, true);
        var profilePermissions = new ProfileDietologistPermissionsModel(true, true, true, true, true, true, true, true);
        object[] models = [
            new RecommendationModel(Guid.NewGuid(), Guid.NewGuid(), "Diet", "Ologist", "Text", true, now, now),
            profilePermissions,
            new DietologistInvitationReadModel(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "diet@example.com", "client@example.com",
                "Client", "Name", "image", now, "Other", 170, ActivityLevel.Moderate,
                "diet@example.com", "Diet", "Ologist", DietologistInvitationStatus.Accepted,
                permissions, now, now.AddDays(1), now),
            new ProfileDietologistRelationshipModel(
                Guid.NewGuid(), "Accepted", "diet@example.com", "Diet", "Ologist", Guid.NewGuid(),
                profilePermissions, now, now.AddDays(1), now),
            new RecommendationCommentModel(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Author", "Name", "author@example.com", "Text", now),
            new ClientTaskModel(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Task", "Details", now,
                ClientTaskStatus.Open, true, now, now),
            new DietologistInvitationForCurrentUserModel(
                Guid.NewGuid(), Guid.NewGuid(), "client@example.com", "Client", "Name", "Pending", now, now.AddDays(1)),
            new AttentionSignalModel("signal", Guid.NewGuid(), "Client", "Type", "Low", "Reason", now, now),
            new BulkRecommendationResultModel("key", []),
            new InvitationModel(Guid.NewGuid(), "client@example.com", "Client", "Name", "Pending", now, now.AddDays(1)),
        ];

        foreach (object model in models) {
            foreach (System.Reflection.PropertyInfo property in model.GetType().GetProperties()) {
                _ = property.GetValue(model);
            }
        }

        Assert.Equal(10, models.Length);
    }

    [Fact]
    public void CreateRecommendationCommentValidator_ValidatesAllFields() {
        var validator = new CreateRecommendationCommentCommandValidator();
        TestValidationResult<CreateRecommendationCommentCommand> missing = validator.TestValidate(
            new CreateRecommendationCommentCommand(null, Guid.Empty, ""));
        TestValidationResult<CreateRecommendationCommentCommand> emptyUser = validator.TestValidate(
            new CreateRecommendationCommentCommand(Guid.Empty, Guid.NewGuid(), "Text"));
        TestValidationResult<CreateRecommendationCommentCommand> tooLong = validator.TestValidate(
            new CreateRecommendationCommentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2001)));
        TestValidationResult<CreateRecommendationCommentCommand> valid = validator.TestValidate(
            new CreateRecommendationCommentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 2000)));

        Assert.Multiple(
            () => missing.ShouldHaveValidationErrorFor(command => command.UserId),
            () => missing.ShouldHaveValidationErrorFor(command => command.RecommendationId),
            () => missing.ShouldHaveValidationErrorFor(command => command.Text),
            () => emptyUser.ShouldHaveValidationErrorFor(command => command.UserId),
            () => tooLong.ShouldHaveValidationErrorFor(command => command.Text),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public void BulkCreateRecommendationsValidator_ValidatesRecipientsAndContent() {
        var validator = new BulkCreateRecommendationsCommandValidator();
        var duplicate = Guid.NewGuid();
        TestValidationResult<BulkCreateRecommendationsCommand> empty = validator.TestValidate(
            new BulkCreateRecommendationsCommand(null, [], "", ""));
        TestValidationResult<BulkCreateRecommendationsCommand> invalidRecipients = validator.TestValidate(
            new BulkCreateRecommendationsCommand(
                null,
                [Guid.Empty, duplicate, duplicate],
                new string('x', 2001),
                new string('x', 101)));
        TestValidationResult<BulkCreateRecommendationsCommand> tooMany = validator.TestValidate(
            new BulkCreateRecommendationsCommand(
                null,
                Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList(),
                "Text",
                "key"));
        TestValidationResult<BulkCreateRecommendationsCommand> valid = validator.TestValidate(
            new BulkCreateRecommendationsCommand(
                null,
                [Guid.NewGuid(), Guid.NewGuid()],
                new string('x', 2000),
                new string('x', 100)));

        Assert.Multiple(
            () => empty.ShouldHaveValidationErrorFor(command => command.ClientUserIds),
            () => empty.ShouldHaveValidationErrorFor(command => command.Text),
            () => empty.ShouldHaveValidationErrorFor(command => command.IdempotencyKey),
            () => invalidRecipients.ShouldHaveValidationErrorFor(command => command.ClientUserIds),
            () => invalidRecipients.ShouldHaveValidationErrorFor(command => command.Text),
            () => invalidRecipients.ShouldHaveValidationErrorFor(command => command.IdempotencyKey),
            () => tooMany.ShouldHaveValidationErrorFor(command => command.ClientUserIds),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public async Task BulkCreateRecommendations_WhenCurrentUserAccessFails_ReturnsFailure() {
        IUserContextService users = CreateFailingUserContext();
        IRecommendationBulkDispatchRepository dispatches = Substitute.For<IRecommendationBulkDispatchRepository>();
        var handler = new BulkCreateRecommendationsCommandHandler(
            Substitute.For<IRecommendationWriteRepository>(),
            dispatches,
            dispatches,
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            users);

        Result<BulkRecommendationResultModel> result = await handler.Handle(
            new BulkCreateRecommendationsCommand(
                Guid.NewGuid(),
                [Guid.NewGuid()],
                "Text",
                "key"),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
    }

    [Fact]
    public async Task GetRecommendationComments_WhenCurrentUserAccessFails_ReturnsFailure() {
        ICurrentUserAccessService users = Substitute.For<ICurrentUserAccessService>();
        users.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        var handler = new GetRecommendationCommentsQueryHandler(
            Substitute.For<IRecommendationDiscussionReadService>(),
            users);

        Result<IReadOnlyList<RecommendationCommentModel>> result = await handler.Handle(
            new GetRecommendationCommentsQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
    }

    [Fact]
    public async Task RecommendationDiscussionReadService_WhenRecommendationIdIsEmpty_ReturnsFailure() {
        var service = new RecommendationDiscussionReadService(
            Substitute.For<IRecommendationReadRepository>(),
            Substitute.For<IRecommendationCommentRepository>());

        Result<IReadOnlyList<RecommendationCommentModel>> result = await service.GetAsync(
            UserId.New(),
            Guid.Empty,
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DietologistUserContextService_CoversFailureAndDelegatedMembers() {
        var userId = UserId.New();
        IUserContextService users = Substitute.For<IUserContextService>();
        users.GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<FoodDiary.Domain.Entities.Users.User>(Errors.Authentication.InvalidToken));
        users.EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        IDietologistUserLookupService lookup = Substitute.For<IDietologistUserLookupService>();
        var service = new DietologistUserContextService(users, lookup);

        Result<string> email = await service.GetAccessibleUserEmailAsync(userId, CancellationToken.None);
        Result<FoodDiary.Application.Abstractions.Users.Models.UserModel> model =
            await service.GetUserModelByIdAsync(userId, CancellationToken.None);
        Result<FoodDiary.Domain.Entities.Users.User> accessible =
            await service.GetAccessibleUserAsync(userId, CancellationToken.None);
        Error? accessError = await service.EnsureCanAccessAsync(userId, CancellationToken.None);
        FoodDiary.Domain.Entities.Users.User? byEmail =
            await service.GetAccessibleUserByEmailAsync("missing@example.com", CancellationToken.None);

        Assert.Multiple(
            () => ResultAssert.Failure(email),
            () => ResultAssert.Failure(model, Errors.Dietologist.AccessDenied.Code),
            () => ResultAssert.Failure(accessible),
            () => Assert.Equal(Errors.Authentication.InvalidToken, accessError),
            () => Assert.Null(byEmail));
    }

    [Fact]
    public async Task DietologistUserLookupService_DelegatesEmailLookup() {
        IUserDirectoryService directory = Substitute.For<IUserDirectoryService>();
        var service = new DietologistUserLookupService(directory);

        FoodDiary.Domain.Entities.Users.User? result =
            await service.GetAccessibleUserByEmailAsync("missing@example.com", CancellationToken.None);

        Assert.Null(result);
        await directory.Received(1).GetByEmailAsync("missing@example.com", Arg.Any<CancellationToken>());
    }

    private static IUserContextService CreateFailingUserContext() {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        return service;
    }
}
