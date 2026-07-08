using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public sealed class CreateRecommendationCommandHandler(
    IDietologistInvitationReadRepository invitationRepository,
    IRecommendationWriteRepository recommendationRepository,
    INotificationWriter notificationWriter,
    INotificationReadRepository notificationRepository,
    INotificationPusher notificationPusher,
    IDietologistUserContextService dietologistUserContextService,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<CreateRecommendationCommand, Result<RecommendationModel>> {
    public async Task<Result<RecommendationModel>> Handle(
        CreateRecommendationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecommendationModel>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Result<User> dietologistResult = await dietologistUserContextService.GetAccessibleUserAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return Result.Failure<RecommendationModel>(dietologistResult.Error);
        }

        Result<UserId> clientUserIdResult = UserIdParser.Parse(
            command.ClientUserId,
            Errors.Validation.Invalid(nameof(command.ClientUserId), "Client user ID is required"));
        if (clientUserIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecommendationModel>(clientUserIdResult);
        }

        UserId clientUserId = clientUserIdResult.Value;

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<RecommendationModel>(accessResult.Error);
        }

        Error? permissionError = DietologistAccessPolicy.EnsureAllPermissions(accessResult.Value);
        if (permissionError is not null) {
            return Result.Failure<RecommendationModel>(permissionError);
        }

        var recommendation = Recommendation.Create(dietologistUserId, clientUserId, command.Text);
        await recommendationRepository.AddAsync(recommendation, cancellationToken).ConfigureAwait(false);
        await NotifyClientAsync(recommendation, dietologistResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(recommendation.ToModel());
    }

    private async Task NotifyClientAsync(Recommendation recommendation, User dietologist, CancellationToken cancellationToken) {
        Notification notification = NotificationFactory.CreateNewRecommendation(
            recommendation.ClientUserId,
            ResolveDietologistLabel(dietologist),
            recommendation.Id.Value.ToString());

        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationRepository,
            notificationPusher,
            recommendation.ClientUserId,
            pushChanged: false);
    }

    private static string ResolveDietologistLabel(User dietologist) {
        string fullName = $"{dietologist.FirstName} {dietologist.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? dietologist.Email
            : $"{fullName} ({dietologist.Email})";
    }
}
