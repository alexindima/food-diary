using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Commands.StartFoodRecognition;

public sealed record StartFoodRecognitionCommand(Guid UserId, Guid Id, Guid ImageAssetId, string? Description)
    : ICommand<Result<FoodRecognitionJobModel>>;
