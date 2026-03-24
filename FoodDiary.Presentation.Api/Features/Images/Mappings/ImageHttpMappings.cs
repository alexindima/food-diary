using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Commands.DeleteImageAsset;
using FoodDiary.Presentation.Api.Features.Images.Requests;

namespace FoodDiary.Presentation.Api.Features.Images.Mappings;

public static class ImageHttpMappings {
    public static DeleteImageAssetCommand ToDeleteCommand(this Guid assetId, Guid userId) =>
        new(userId, assetId);

    public static GetImageUploadUrlCommand ToCommand(this GetImageUploadUrlHttpRequest request, Guid userId) {
        return new GetImageUploadUrlCommand(
            userId,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes);
    }
}
