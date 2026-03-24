using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed record AnalyzeFoodImageCommand(Guid UserId, Guid ImageAssetId, string? Description)
    : IQuery<Result<FoodVisionModel>>;
