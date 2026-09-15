using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyRecommendations;

public record GetMyRecommendationsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<RecommendationModel>>>, IUserRequest;
