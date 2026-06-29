using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public class CreateRecommendationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IRecommendationRepository recommendationRepository,
    INotificationWriter notificationWriter,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IUserRepository userRepository)
    : ICommandHandler<CreateRecommendationCommand, Result<RecommendationModel>> {
    public async Task<Result<RecommendationModel>> Handle(
        CreateRecommendationCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecommendationModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        Error? currentUserAccessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(
            userRepository, dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<RecommendationModel>(currentUserAccessError);
        }

        var clientUserId = new UserId(command.ClientUserId);

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
        await NotifyClientAsync(recommendation, cancellationToken).ConfigureAwait(false);

        return Result.Success(recommendation.ToModel());
    }

    private async Task NotifyClientAsync(Recommendation recommendation, CancellationToken cancellationToken) {
        User? dietologist = await userRepository.GetByIdAsync(recommendation.DietologistUserId, cancellationToken).ConfigureAwait(false);
        Notification notification = NotificationFactory.CreateNewRecommendation(
            recommendation.ClientUserId,
            ResolveDietologistLabel(dietologist),
            recommendation.Id.Value.ToString());

        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);

        int unreadCount = await notificationRepository.GetUnreadCountAsync(recommendation.ClientUserId, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushUnreadCountAsync(recommendation.ClientUserId.Value, unreadCount, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveDietologistLabel(User? dietologist) {
        if (dietologist is null) {
            return string.Empty;
        }

        string fullName = $"{dietologist.FirstName} {dietologist.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? dietologist.Email
            : $"{fullName} ({dietologist.Email})";
    }
}
