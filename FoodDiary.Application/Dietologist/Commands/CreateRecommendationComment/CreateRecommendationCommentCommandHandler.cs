using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationComment;

public sealed class CreateRecommendationCommentCommandHandler(
    IRecommendationReadRepository recommendationRepository,
    IRecommendationCommentRepository commentRepository,
    IDietologistInvitationReadModelRepository invitationRepository,
    INotificationWriter notificationWriter,
    IAuditEntryWriter auditWriter,
    IUserContextService userContextService)
    : ICommandHandler<CreateRecommendationCommentCommand, Result<RecommendationCommentModel>> {
    public async Task<Result<RecommendationCommentModel>> Handle(
        CreateRecommendationCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecommendationCommentModel>(userIdResult);
        }

        Result<RecommendationId> recommendationIdResult = RequiredIdParser.Parse(
            command.RecommendationId,
            nameof(command.RecommendationId),
            "Recommendation id must not be empty.",
            value => new RecommendationId(value));
        if (recommendationIdResult.IsFailure) {
            return Result.Failure<RecommendationCommentModel>(recommendationIdResult.Error);
        }

        Recommendation? recommendation = await recommendationRepository.GetByIdAsync(
            recommendationIdResult.Value,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        UserId authorUserId = userIdResult.Value;
        if (recommendation is null ||
            (recommendation.ClientUserId != authorUserId && recommendation.DietologistUserId != authorUserId)) {
            return Result.Failure<RecommendationCommentModel>(Errors.Dietologist.InvitationNotFound);
        }

        Result accessResult = await EnsureCanPostAsync(
            recommendation, authorUserId, cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure<RecommendationCommentModel>(accessResult.Error);
        }

        Result<User> authorResult = await userContextService.GetAccessibleUserAsync(
            authorUserId, cancellationToken).ConfigureAwait(false);
        if (authorResult.IsFailure) {
            return Result.Failure<RecommendationCommentModel>(authorResult.Error);
        }

        var comment = RecommendationComment.Create(
            recommendation.Id,
            authorUserId,
            command.Text);
        await commentRepository.AddAsync(comment, cancellationToken).ConfigureAwait(false);

        UserId recipientUserId = authorUserId == recommendation.ClientUserId
            ? recommendation.DietologistUserId
            : recommendation.ClientUserId;
        Notification notification = NotificationFactory.CreateNewRecommendationComment(
            recipientUserId,
            recommendation.Id.Value.ToString(),
            recommendation.ClientUserId.Value.ToString(),
            forDietologist: authorUserId == recommendation.ClientUserId);
        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
        await WriteAuditAsync(recommendation, authorUserId, cancellationToken).ConfigureAwait(false);

        return Result.Success(ToModel(comment, authorResult.Value));
    }

    private Task WriteAuditAsync(
        Recommendation recommendation,
        UserId authorUserId,
        CancellationToken cancellationToken) =>
        auditWriter.AddAsync(
            authorUserId,
            recommendation.ClientUserId.Value,
            "dietologist.recommendation.comment.created",
            "Recommendation",
            recommendation.Id.Value.ToString(),
            metadata: null,
            cancellationToken);

    private async Task<Result> EnsureCanPostAsync(
        Recommendation recommendation,
        UserId authorUserId,
        CancellationToken cancellationToken) {
        if (recommendation.DietologistUserId != authorUserId) {
            return Result.Success();
        }

        Result<DietologistPermissionsModel> accessResult =
            await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
                invitationRepository,
                recommendation.DietologistUserId,
                recommendation.ClientUserId,
                cancellationToken).ConfigureAwait(false);
        return accessResult.IsFailure
            ? Result.Failure(accessResult.Error)
            : Result.Success();
    }

    private static RecommendationCommentModel ToModel(RecommendationComment comment, User author) =>
        new(
            comment.Id.Value,
            comment.RecommendationId.Value,
            comment.AuthorUserId.Value,
            author.FirstName,
            author.LastName,
            author.Email,
            comment.Text,
            comment.CreatedOnUtc);
}
