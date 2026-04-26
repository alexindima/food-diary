using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;

public record GetMyRecommendationsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<RecommendationModel>>>, IUserRequest;
