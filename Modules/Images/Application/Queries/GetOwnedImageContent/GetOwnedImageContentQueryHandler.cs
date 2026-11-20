using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Queries.GetOwnedImageContent;

public sealed class GetOwnedImageContentQueryHandler(IImageAssetContentService content)
    : IQueryHandler<GetOwnedImageContentQuery, Result<string>> {
    public Task<Result<string>> Handle(GetOwnedImageContentQuery query, CancellationToken cancellationToken) {
        Result<UserId> owner = UserIdParser.Parse(query.UserId, ImageErrors.InvalidData("UserId is required."));
        if (owner.IsFailure) {
            return Task.FromResult(UserIdParser.ToFailure<string>(owner));
        }

        Result<ImageAssetId> asset = ImageAssetIdParser.ParseRequired(query.AssetId, ImageErrors.InvalidData("AssetId is required."));
        return asset.IsFailure
            ? Task.FromResult(Result.Failure<string>(asset.Error))
            : content.GetDataUrlAsync(asset.Value, owner.Value, cancellationToken);
    }
}
