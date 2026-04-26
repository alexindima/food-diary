using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed record DeleteImageAssetCommand(Guid UserId, Guid AssetId) : IRequest<Result>;
