using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed record AnalyzeFoodImageCommand(Guid UserId, Guid ImageAssetId, string? Description)
    : ICommand<Result<FoodVisionModel>>;
