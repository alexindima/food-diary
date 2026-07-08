using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;

public record MarkRecommendationReadCommand(
    Guid? UserId,
    Guid RecommendationId) : ICommand<Result>, IUserRequest;
