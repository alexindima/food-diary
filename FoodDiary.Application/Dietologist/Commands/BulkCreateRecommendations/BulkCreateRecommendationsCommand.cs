using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.BulkCreateRecommendations;

public sealed record BulkCreateRecommendationsCommand(
    Guid? UserId,
    IReadOnlyList<Guid> ClientUserIds,
    string Text,
    string IdempotencyKey) : ICommand<Result<BulkRecommendationResultModel>>, IUserRequest;
