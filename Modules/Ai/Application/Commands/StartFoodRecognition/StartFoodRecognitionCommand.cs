using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.StartFoodRecognition;

public sealed record StartFoodRecognitionCommand(Guid UserId, Guid Id, Guid ImageAssetId, string? Description, bool IsProductLabel = false, IReadOnlyList<Guid>? AdditionalImageAssetIds = null)
    : ICommand<Result<FoodRecognitionJobModel>>;
