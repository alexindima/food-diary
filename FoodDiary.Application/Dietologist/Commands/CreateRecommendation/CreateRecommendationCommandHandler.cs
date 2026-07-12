using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public sealed class CreateRecommendationCommandHandler(
    IDietologistInvitationReadRepository invitationRepository,
    IRecommendationWriteRepository recommendationRepository,
    IDietologistUserContextService dietologistUserContextService)
    : ICommandHandler<CreateRecommendationCommand, Result<RecommendationModel>> {
    public async Task<Result<RecommendationModel>> Handle(
        CreateRecommendationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            dietologistUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecommendationModel>(userIdResult);
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

        return Result.Success(recommendation.ToModel());
    }
}
