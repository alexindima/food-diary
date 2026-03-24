using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed record AnalyzeFoodImageCommand(Guid UserId, ImageAssetId ImageAssetId, string? Description)
    : IQuery<Result<FoodVisionModel>>;
