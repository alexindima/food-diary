using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;

public sealed record AnalyzeFoodImageCommand(Guid UserId, Guid ImageAssetId, string? Description, string RequestId)
    : ICommand<Result<FoodVisionModel>>;
