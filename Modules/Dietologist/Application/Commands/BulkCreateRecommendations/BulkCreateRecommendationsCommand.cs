using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.BulkCreateRecommendations;

public sealed record BulkCreateRecommendationsCommand(
    Guid? UserId,
    IReadOnlyList<Guid> ClientUserIds,
    string Text,
    string IdempotencyKey) : ICommand<Result<BulkRecommendationResultModel>>, IUserRequest;
