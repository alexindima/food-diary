using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed record DeleteImageAssetCommand(Guid UserId, Guid AssetId) : IRequest<Result>;
