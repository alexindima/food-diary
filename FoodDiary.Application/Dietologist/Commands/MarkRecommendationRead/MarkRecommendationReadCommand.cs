using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;

public record MarkRecommendationReadCommand(
    Guid? UserId,
    Guid RecommendationId) : ICommand<Result>, IUserRequest;
