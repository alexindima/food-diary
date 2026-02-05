using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;

public sealed record AnalyzeFoodImageCommand(UserId UserId, ImageAssetId ImageAssetId)
    : IQuery<Result<FoodVisionResponse>>;
