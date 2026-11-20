using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Queries.GetOwnedImageContent;

public sealed record GetOwnedImageContentQuery(Guid UserId, Guid AssetId) : IQuery<Result<string>>;
