using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed record DeleteImageAssetCommand(Guid UserId, Guid AssetId) : ICommand<Result>;
