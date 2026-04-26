using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;

namespace FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;

public class GetMyRecommendationsQueryHandler(
    IRecommendationRepository recommendationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetMyRecommendationsQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetMyRecommendationsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessError);
        }

        var recommendations = await recommendationRepository.GetByClientAsync(userId, cancellationToken: cancellationToken);
        var models = recommendations.Select(r => r.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }
}
