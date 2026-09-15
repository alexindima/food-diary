using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.MarkRecommendationRead;

public record MarkRecommendationReadCommand(
    Guid? UserId,
    Guid RecommendationId) : ICommand<Result>, IUserRequest;
